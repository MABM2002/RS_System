# Guía de Migración: Lógica C# → Stored Procedures PostgreSQL

**Proyecto**: RS_System — ASP.NET Core 8 + EF Core + PostgreSQL  
**Fecha**: 3 de junio de 2026  
**Enfoque**: Performance — reducir round-trips a la base de datos  
**Alcance**: Balanceado — 9 operaciones (5 SPs de alta prioridad + 4 funciones de reportes)

---

## Índice

1. [Infraestructura Existente](#1-infraestructura-existente)
2. [SP-01: `guardar_movimientos_bulk`](#2-sp-01-guardar_movimientos_bulk)
3. [SP-02: `registrar_colaboracion`](#3-sp-02-registrar_colaboracion)
4. [SP-03: `upsert_movimiento_diario`](#4-sp-03-upsert_movimiento_diario)
5. [SP-04: `procesar_cierre_diario`](#5-sp-04-procesar_cierre_diario)
6. [SP-05: `cerrar_cierre_diezmo`](#5-sp-05-cerrar_cierre_diezmo)
7. [FN-06: `fn_balance_general_recursivo`](#7-fn-06-fn_balance_general_recursivo)
8. [FN-07: `fn_balance_general` + `fn_estado_resultados`](#8-fn-07-fn_balance_general--fn_estado_resultados)
9. [FN-08: `fn_reporte_colaboraciones` + `fn_estado_cuenta_miembro`](#9-fn-08-fn_reporte_colaboraciones--fn_estado_cuenta_miembro)
10. [Estrategia de Migración](#10-estrategia-de-migración)

---

## 1. Infraestructura Existente

El proyecto **ya cuenta** con el ejecutor de SPs. No se necesita instalar nada nuevo.

### Archivo: `RS_system/Data/PostgresDirectExecutor.cs`

```csharp
// Interfaz registrada en DI — se inyecta en cualquier Service:
public interface IPostgresDirectExecutor
{
    Task ExecuteStoredProcedureAsync(string procedureName, params NpgsqlParameter[] parameters);
    Task<NpgsqlParameter[]> ExecuteStoredProcedureWithOutputAsync(string procedureName, params NpgsqlParameter[] parameters);
    Task<T> ExecuteStoredProcedureScalarAsync<T>(string procedureName, params NpgsqlParameter[] parameters);
    Task<DataTable> ExecuteStoredProcedureDataTableAsync(string procedureName, params NpgsqlParameter[] parameters);
    Task<List<T>> ExecuteStoredProcedureListAsync<T>(string procedureName, Func<NpgsqlDataReader, T> map, params NpgsqlParameter[] parameters);
}
```

### SPs ya en uso (referencia):

| SP | Llamado desde | Propósito |
|---|---|---|
| `recalcular_saldo_por_id` | `ContabilidadGeneralService.CalcularSaldoActualAsync` | Recalcula saldo con output params |
| `recalcular_saldos_mensuales` | `ContabilidadGeneralService.CerrarReporteAsync` | Recalcula saldos mensuales |

### Cómo llamar un SP desde C# (patrón a seguir en todas las migraciones):

```csharp
// Ejemplo: SP sin retorno (ExecuteNonQuery)
await _postgresExecutor.ExecuteStoredProcedureAsync("nombre_sp",
    new NpgsqlParameter("p_param1", 123),
    new NpgsqlParameter("p_param2", "valor"));

// Ejemplo: SP con output params
var parameters = new[] {
    new NpgsqlParameter("p_id", 123),
    new NpgsqlParameter("p_resultado", NpgsqlDbType.Numeric) { Direction = ParameterDirection.Output }
};
var results = await _postgresExecutor.ExecuteStoredProcedureWithOutputAsync("nombre_sp", parameters);
var valor = Convert.ToDecimal(results[1].Value);

// Ejemplo: SP que retorna un valor escalar
var total = await _postgresExecutor.ExecuteStoredProcedureScalarAsync<decimal>("nombre_sp",
    new NpgsqlParameter("p_id", 123));
```

---

## 2. SP-01: `guardar_movimientos_bulk`

### 📍 Ubicación actual

| Concepto | Referencia |
|---|---|
| **Archivo** | `RS_system/Services/ContabilidadGeneralService.cs` |
| **Método** | `GuardarMovimientosBulkAsync` (línea ~224) |
| **Interfaz** | `IContabilidadGeneralService.GuardarMovimientosBulkAsync` |
| **Modelos** | `MovimientoGeneral`, `PartidaContable`, `DetallePartidaContable`, `CategoriaIngreso`, `CategoriaEgreso` |
| **Tablas** | `movimientos_generales`, `partidas_contables`, `detalles_partida_contable` |

### 🔴 Problema

- **4 `SaveChangesAsync`** en una sola operación sin transacción explícita
- Flujo: eliminar partidas antiguas → eliminar movimientos antiguos → insertar nuevos → generar nuevas partidas
- Si falla en el paso 3, la BD queda inconsistente (partidas huérfanas o movimientos a medias)
- Itera cada movimiento con una query adicional para generar su partida doble

### ✅ Solución propuesta

Todo el flujo en **1 sola transacción atómica** dentro del SP. Los movimientos se envían como **JSONB** para evitar múltiples parámetros.

### 📝 SQL del Stored Procedure

```sql
-- ============================================================================
-- SP-01: guardar_movimientos_bulk
-- Reemplaza: ContabilidadGeneralService.GuardarMovimientosBulkAsync
-- Descripción: Reemplaza todos los movimientos de un reporte mensual
--              y genera sus partidas contables en una sola transacción.
-- ============================================================================

CREATE OR REPLACE FUNCTION guardar_movimientos_bulk(
    p_reporte_id        BIGINT,
    p_movimientos_json  JSONB,   -- Array de objetos MovimientoGeneral
    p_usuario           VARCHAR(100)
)
RETURNS TABLE(
    success         BOOLEAN,
    message         TEXT,
    movimientos_count INTEGER,
    partidas_count  INTEGER
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_reporte           RECORD;
    v_cuenta_caja_id    BIGINT;
    v_periodo_id        BIGINT;
    v_mov               JSONB;
    v_nuevo_mov_id      BIGINT;
    v_partida_id        BIGINT;
    v_categoria_id      BIGINT;
    v_cuenta_cat_id     BIGINT;
    v_tipo              INTEGER;
    v_monto             NUMERIC(18,2);
    v_fecha             TIMESTAMP;
    v_descripcion       VARCHAR(200);
    v_comprobante       VARCHAR(50);
    v_mov_count         INTEGER := 0;
    v_part_count        INTEGER := 0;
BEGIN
    -- 1. Validar que el reporte existe y no está cerrado
    SELECT id, mes, anio, cerrado
    INTO v_reporte
    FROM reportes_mensuales_generales
    WHERE id = p_reporte_id;

    IF v_reporte.id IS NULL THEN
        RETURN QUERY SELECT false, 'Reporte no encontrado', 0, 0;
        RETURN;
    END IF;

    IF v_reporte.cerrado THEN
        RETURN QUERY SELECT false, 'El reporte ya está cerrado', 0, 0;
        RETURN;
    END IF;

    -- 2. Obtener cuenta caja por defecto (código 1.1.01 o primera cuenta de activo)
    SELECT cc.id INTO v_cuenta_caja_id
    FROM cuentas_contables cc
    JOIN account_types at ON cc.account_type_id = at.id
    WHERE cc.activa = true AND cc.codigo = '1.1.01'
    LIMIT 1;

    IF v_cuenta_caja_id IS NULL THEN
        -- Fallback: primera cuenta deudora activa
        SELECT cc.id INTO v_cuenta_caja_id
        FROM cuentas_contables cc
        JOIN account_types at ON cc.account_type_id = at.id
        WHERE cc.activa = true AND at.naturaleza = 0  -- Deudora
        ORDER BY cc.codigo LIMIT 1;
    END IF;

    IF v_cuenta_caja_id IS NULL THEN
        RETURN QUERY SELECT false, 'No se encontró cuenta de caja por defecto', 0, 0;
        RETURN;
    END IF;

    -- 3. Obtener o crear período contable
    SELECT id INTO v_periodo_id
    FROM periodos_contables
    WHERE mes = v_reporte.mes AND anio = v_reporte.anio;

    IF v_periodo_id IS NULL THEN
        INSERT INTO periodos_contables (mes, anio, fecha_inicio, fecha_fin, saldo_inicial, fecha_creacion, cerrado)
        VALUES (
            v_reporte.mes, v_reporte.anio,
            MAKE_DATE(v_reporte.anio, v_reporte.mes, 1),
            (MAKE_DATE(v_reporte.anio, v_reporte.mes, 1) + INTERVAL '1 month' - INTERVAL '1 day')::DATE,
            0, NOW(), false
        )
        RETURNING id INTO v_periodo_id;
    END IF;

    -- ═══════════════════════════════════════════════════════════════
    -- 4. TRANSACCIÓN ATÓMICA: eliminar antiguos, insertar nuevos, generar partidas
    -- ═══════════════════════════════════════════════════════════════

    -- 4a. Eliminar partidas contables asociadas a movimientos antiguos
    DELETE FROM detalles_partida_contable
    WHERE partida_contable_id IN (
        SELECT id FROM partidas_contables
        WHERE movimiento_general_id IN (
            SELECT id FROM movimientos_generales
            WHERE reporte_mensual_general_id = p_reporte_id
        )
    );

    DELETE FROM partidas_contables
    WHERE movimiento_general_id IN (
        SELECT id FROM movimientos_generales
        WHERE reporte_mensual_general_id = p_reporte_id
    );

    -- 4b. Eliminar movimientos antiguos
    DELETE FROM movimientos_generales
    WHERE reporte_mensual_general_id = p_reporte_id;

    -- 4c. Procesar cada movimiento del JSON
    FOR v_mov IN SELECT * FROM jsonb_array_elements(p_movimientos_json)
    LOOP
        v_tipo        := (v_mov->>'tipo')::INTEGER;
        v_monto       := (v_mov->>'monto')::NUMERIC(18,2);
        v_fecha       := (v_mov->>'fecha')::TIMESTAMP;
        v_descripcion := COALESCE(v_mov->>'descripcion', '');
        v_comprobante := v_mov->>'numero_comprobante';

        -- Validaciones básicas
        IF v_monto <= 0 OR v_descripcion IS NULL OR v_descripcion = '' THEN
            RETURN QUERY SELECT false,
                'Movimiento inválido: monto=' || v_monto || ' desc=' || v_descripcion,
                v_mov_count, v_part_count;
            RETURN;
        END IF;

        -- Insertar movimiento
        INSERT INTO movimientos_generales (
            reporte_mensual_general_id, tipo,
            categoria_ingreso_id, categoria_egreso_id,
            monto, fecha, descripcion, numero_comprobante
        ) VALUES (
            p_reporte_id, v_tipo,
            CASE WHEN v_tipo = 1 THEN (v_mov->>'categoria_ingreso_id')::BIGINT ELSE NULL END,
            CASE WHEN v_tipo = 2 THEN (v_mov->>'categoria_egreso_id')::BIGINT ELSE NULL END,
            v_monto, v_fecha, v_descripcion, v_comprobante
        )
        RETURNING id INTO v_nuevo_mov_id;

        v_mov_count := v_mov_count + 1;

        -- 4d. Generar partida contable (doble entrada)
        IF v_tipo = 1 THEN
            -- INGRESO: Débito a Caja, Crédito a cuenta de la categoría
            SELECT cuenta_contable_id INTO v_cuenta_cat_id
            FROM categorias_ingreso WHERE id = (v_mov->>'categoria_ingreso_id')::BIGINT;

            IF v_cuenta_cat_id IS NULL THEN
                RETURN QUERY SELECT false,
                    'Categoría de ingreso sin cuenta contable: ' || (v_mov->>'categoria_ingreso_id'),
                    v_mov_count, v_part_count;
                RETURN;
            END IF;

            -- Insertar partida
            INSERT INTO partidas_contables (
                fecha, referencia, descripcion, periodo_contable_id, movimiento_general_id, cerrada, fecha_creacion
            ) VALUES (
                v_fecha,
                COALESCE(v_comprobante, 'MOV-' || v_nuevo_mov_id),
                v_descripcion,
                v_periodo_id, v_nuevo_mov_id, false, NOW()
            )
            RETURNING id INTO v_partida_id;

            -- Detalle: Débito a Caja
            INSERT INTO detalles_partida_contable (partida_contable_id, cuenta_contable_id, debito, credito)
            VALUES (v_partida_id, v_cuenta_caja_id, v_monto, 0);

            -- Detalle: Crédito a cuenta de categoría
            INSERT INTO detalles_partida_contable (partida_contable_id, cuenta_contable_id, debito, credito)
            VALUES (v_partida_id, v_cuenta_cat_id, 0, v_monto);

        ELSIF v_tipo = 2 THEN
            -- EGRESO: Débito a cuenta de categoría, Crédito a Caja
            SELECT cuenta_contable_id INTO v_cuenta_cat_id
            FROM categorias_egreso WHERE id = (v_mov->>'categoria_egreso_id')::BIGINT;

            IF v_cuenta_cat_id IS NULL THEN
                RETURN QUERY SELECT false,
                    'Categoría de egreso sin cuenta contable: ' || (v_mov->>'categoria_egreso_id'),
                    v_mov_count, v_part_count;
                RETURN;
            END IF;

            INSERT INTO partidas_contables (
                fecha, referencia, descripcion, periodo_contable_id, movimiento_general_id, cerrada, fecha_creacion
            ) VALUES (
                v_fecha,
                COALESCE(v_comprobante, 'MOV-' || v_nuevo_mov_id),
                v_descripcion,
                v_periodo_id, v_nuevo_mov_id, false, NOW()
            )
            RETURNING id INTO v_partida_id;

            -- Detalle: Débito a cuenta de categoría
            INSERT INTO detalles_partida_contable (partida_contable_id, cuenta_contable_id, debito, credito)
            VALUES (v_partida_id, v_cuenta_cat_id, v_monto, 0);

            -- Detalle: Crédito a Caja
            INSERT INTO detalles_partida_contable (partida_contable_id, cuenta_contable_id, debito, credito)
            VALUES (v_partida_id, v_cuenta_caja_id, 0, v_monto);
        END IF;

        v_part_count := v_part_count + 1;
    END LOOP;

    RETURN QUERY SELECT true, 'OK', v_mov_count, v_part_count;
END;
$$;
```

### 🔧 Cambios en C#

**Archivo**: `RS_system/Services/ContabilidadGeneralService.cs`

Reemplazar TODO el cuerpo de `GuardarMovimientosBulkAsync` por:

```csharp
public async Task<bool> GuardarMovimientosBulkAsync(long reporteId, List<MovimientoGeneral> movimientos)
{
    try
    {
        if (movimientos == null || !movimientos.Any())
            return false;

        // Convertir movimientos a JSONB para enviar al SP
        var movimientosJson = System.Text.Json.JsonSerializer.Serialize(
            movimientos.Select(m => new
            {
                tipo = m.Tipo,
                categoria_ingreso_id = m.CategoriaIngresoId,
                categoria_egreso_id = m.CategoriaEgresoId,
                monto = m.Monto,
                fecha = m.Fecha.ToString("yyyy-MM-ddTHH:mm:ss"),
                descripcion = m.Descripcion,
                numero_comprobante = m.NumeroComprobante
            })
        );

        // Llamar al SP
        var dt = await _postgresExecutor.ExecuteStoredProcedureDataTableAsync(
            "guardar_movimientos_bulk",
            new NpgsqlParameter("p_reporte_id", reporteId),
            new NpgsqlParameter("p_movimientos_json", NpgsqlTypes.NpgsqlDbType.Jsonb)
                { Value = movimientosJson },
            new NpgsqlParameter("p_usuario", "")
        );

        if (dt.Rows.Count > 0)
        {
            var success = (bool)dt.Rows[0]["success"];
            if (!success)
            {
                var msg = (string)dt.Rows[0]["message"];
                throw new InvalidOperationException(msg);
            }
        }

        return true;
    }
    catch (Exception ex)
    {
        // Log the error
        return false;
    }
}
```

**IMPORTANTE**: Agregar `using System.Text.Json;` al inicio del archivo.

> ⚠️ **Nota sobre DI**: `ContabilidadGeneralService` ya recibe `IPostgresDirectExecutor` en su constructor (línea 12: `IPostgresDirectExecutor postgresExecutor`). No se necesita modificar el constructor.

### 📊 Ganancia estimada

| Métrica | Antes | Después |
|---|---|---|
| Round-trips a BD | 4 + N (uno por movimiento) | 1 |
| Atomicidad | ❌ Sin transacción explícita | ✅ Todo en una transacción |
| Tiempo estimado (20 movimientos) | ~24 queries | 1 query |

---

## 3. SP-02: `registrar_colaboracion`

### 📍 Ubicación actual

| Concepto | Referencia |
|---|---|
| **Archivo** | `RS_system/Services/ColaboracionService.cs` |
| **Métodos** | `RegistrarColaboracionAsync` (línea ~42), `DistribuirMonto` (línea ~130), `GenerarRangoMeses` (línea ~247) |
| **Interfaz** | `IColaboracionService.RegistrarColaboracionAsync` |
| **Modelos** | `RegistrarColaboracionViewModel`, `Colaboracion`, `DetalleColaboracion`, `ColaboracionHead` |
| **Tablas** | `colaboraciones`, `detalle_colaboraciones`, `colaboracion_heads`, `tipos_colaboracion` |

### 🔴 Problema

- El algoritmo `DistribuirMonto` itera anidado sobre meses y tipos en C# (puramente aritmético)
- 2 `SaveChangesAsync` (colaboración + update del head)
- `GenerarRangoMeses` construye una lista en memoria cuando SQL puede generar la serie con `generate_series()`

### ✅ Solución propuesta

SP que recibe los parámetros del ViewModel, genera la distribución de montos en PL/pgSQL, inserta colaboración + detalles + actualiza head en una transacción.

### 📝 SQL del Stored Procedure

```sql
-- ============================================================================
-- SP-02: registrar_colaboracion
-- Reemplaza: ColaboracionService.RegistrarColaboracionAsync + DistribuirMonto
-- Descripción: Registra una colaboración completa con distribución automática
--              de montos entre meses y tipos de colaboración.
-- ============================================================================

CREATE OR REPLACE FUNCTION registrar_colaboracion(
    p_miembro_id            BIGINT,
    p_monto_total           NUMERIC(12,2),
    p_tipos_ids_json        JSONB,      -- Array de IDs de tipos: [1, 2, 3]
    p_anio_inicial          INTEGER,
    p_mes_inicial           INTEGER,
    p_anio_final            INTEGER,
    p_mes_final             INTEGER,
    p_tipo_prioritario_id   BIGINT,     -- Puede ser NULL
    p_jornada_id            INTEGER,
    p_observaciones         VARCHAR(500),
    p_registrado_por        VARCHAR(100)
)
RETURNS TABLE(
    success         BOOLEAN,
    message         TEXT,
    colaboracion_id BIGINT
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_head              RECORD;
    v_fecha_inicial     DATE;
    v_fecha_final       DATE;
    v_colaboracion_id   BIGINT;
    v_monto_restante    NUMERIC(12,2);
    v_tipo_id           BIGINT;
    v_monto_sugerido    NUMERIC(12,2);
    v_monto_a_asignar   NUMERIC(12,2);
    v_anio              INTEGER;
    v_mes               INTEGER;
    v_tipos_ids         BIGINT[];
    v_tipos_ordenados   BIGINT[];
    v_idx               INTEGER;
    v_fecha_loop        DATE;
    v_tipos_json        JSONB;
    v_tipo_rec          RECORD;
BEGIN
    -- 1. Validar rango de fechas
    v_fecha_inicial := MAKE_DATE(p_anio_inicial, p_mes_inicial, 1);
    v_fecha_final   := MAKE_DATE(p_anio_final, p_mes_final, 1);

    IF v_fecha_final < v_fecha_inicial THEN
        RETURN QUERY SELECT false, 'La fecha final no puede ser anterior a la fecha inicial', NULL::BIGINT;
        RETURN;
    END IF;

    -- 2. Obtener o validar la jornada (ColaboracionHead)
    IF p_jornada_id > 0 THEN
        SELECT * INTO v_head FROM colaboracion_heads WHERE id = p_jornada_id;
    ELSE
        SELECT * INTO v_head FROM colaboracion_heads
        WHERE fecha = CURRENT_DATE
        LIMIT 1;
    END IF;

    IF v_head.id IS NULL THEN
        -- Crear nueva jornada
        INSERT INTO colaboracion_heads (fecha, total, creado_en, actualizado_en, creado_por)
        VALUES (CURRENT_DATE, 0, NOW(), NOW(), p_registrado_por)
        RETURNING * INTO v_head;
    END IF;

    IF v_head.es_cerrado THEN
        RETURN QUERY SELECT false,
            'La jornada del ' || v_head.fecha::TEXT || ' está cerrada', NULL::BIGINT;
        RETURN;
    END IF;

    -- 3. Obtener tipos de colaboración seleccionados con sus montos sugeridos
    --    y ordenarlos (prioritario primero si se especificó)
    CREATE TEMP TABLE IF NOT EXISTS _tipos_ordenados (
        orden       INTEGER,
        tipo_id     BIGINT,
        monto_sug   NUMERIC(12,2)
    ) ON COMMIT DROP;

    DELETE FROM _tipos_ordenados;

    -- Insertar tipos en orden: prioritario primero, luego el resto por orden natural
    INSERT INTO _tipos_ordenados (orden, tipo_id, monto_sug)
    SELECT
        CASE WHEN tc.id = p_tipo_prioritario_id THEN 0 ELSE tc.orden END,
        tc.id,
        COALESCE(tc.monto_sugerido, 0)
    FROM tipos_colaboracion tc
    WHERE tc.id IN (SELECT (jsonb_array_elements(p_tipos_ids_json))::BIGINT)
      AND tc.activo = true
    ORDER BY
        CASE WHEN tc.id = p_tipo_prioritario_id THEN 0 ELSE 1 END,
        tc.orden;

    -- 4. Insertar colaboración principal
    INSERT INTO colaboraciones (
        miembro_id, colaboracion_head_id, fecha_registro, monto_total,
        observaciones, registrado_por, creado_en, actualizado_en
    ) VALUES (
        p_miembro_id, v_head.id, NOW(), p_monto_total,
        p_observaciones, p_registrado_por, NOW(), NOW()
    )
    RETURNING id INTO v_colaboracion_id;

    -- 5. Distribuir monto entre meses × tipos
    v_monto_restante := p_monto_total;
    v_fecha_loop     := v_fecha_inicial;

    <<meses_loop>>
    WHILE v_fecha_loop <= v_fecha_final AND v_monto_restante > 0 LOOP
        v_anio := EXTRACT(YEAR FROM v_fecha_loop);
        v_mes  := EXTRACT(MONTH FROM v_fecha_loop);

        <<tipos_loop>>
        FOR v_tipo_rec IN SELECT * FROM _tipos_ordenados ORDER BY orden LOOP
            IF v_monto_restante <= 0 THEN
                EXIT tipos_loop;
            END IF;

            -- Asignar min(monto_sugerido, monto_restante)
            v_monto_a_asignar := LEAST(v_tipo_rec.monto_sug, v_monto_restante);

            IF v_monto_a_asignar > 0 THEN
                INSERT INTO detalle_colaboraciones (
                    colaboracion_id, tipo_colaboracion_id,
                    mes, anio, monto, creado_en
                ) VALUES (
                    v_colaboracion_id, v_tipo_rec.tipo_id,
                    v_mes, v_anio, v_monto_a_asignar, NOW()
                );

                v_monto_restante := v_monto_restante - v_monto_a_asignar;
            END IF;
        END LOOP tipos_loop;

        v_fecha_loop := v_fecha_loop + INTERVAL '1 month';
    END LOOP meses_loop;

    -- 6. Actualizar total del head
    UPDATE colaboracion_heads
    SET total = total + p_monto_total,
        actualizado_en = NOW()
    WHERE id = v_head.id;

    -- 7. Limpiar tabla temporal
    DROP TABLE IF EXISTS _tipos_ordenados;

    RETURN QUERY SELECT true, 'Colaboración registrada exitosamente', v_colaboracion_id;
END;
$$;
```

### 🔧 Cambios en C#

**Archivo**: `RS_system/Services/ColaboracionService.cs`

Agregar `IPostgresDirectExecutor` al constructor y reemplazar `RegistrarColaboracionAsync`:

```csharp
// Modificar el constructor (añadir IPostgresDirectExecutor):
private readonly IPostgresDirectExecutor _pg;

public ColaboracionService(
    ApplicationDbContext context,
    IContabilidadPartidaDobleService contabilidadPartidaDoble,
    IPostgresDirectExecutor pg)   // ← NUEVO
{
    _context = context;
    _contabilidadPartidaDoble = contabilidadPartidaDoble;
    _pg = pg;                     // ← NUEVO
}

// Reemplazar TODO el método RegistrarColaboracionAsync:
public async Task<Colaboracion> RegistrarColaboracionAsync(
    RegistrarColaboracionViewModel model,
    string registradoPor)
{
    var tiposJson = System.Text.Json.JsonSerializer.Serialize(model.TiposSeleccionados);

    var dt = await _pg.ExecuteStoredProcedureDataTableAsync(
        "registrar_colaboracion",
        new NpgsqlParameter("p_miembro_id", model.MiembroId),
        new NpgsqlParameter("p_monto_total", model.MontoTotal),
        new NpgsqlParameter("p_tipos_ids_json", NpgsqlTypes.NpgsqlDbType.Jsonb)
            { Value = tiposJson },
        new NpgsqlParameter("p_anio_inicial", model.AnioInicial),
        new NpgsqlParameter("p_mes_inicial", model.MesInicial),
        new NpgsqlParameter("p_anio_final", model.AnioFinal),
        new NpgsqlParameter("p_mes_final", model.MesFinal),
        new NpgsqlParameter("p_tipo_prioritario_id",
            model.TipoPrioritario.HasValue ? model.TipoPrioritario.Value : DBNull.Value),
        new NpgsqlParameter("p_jornada_id", model.IdJornada),
        new NpgsqlParameter("p_observaciones",
            model.Observaciones ?? (object)DBNull.Value),
        new NpgsqlParameter("p_registrado_por", registradoPor)
    );

    if (dt.Rows.Count == 0 || !(bool)dt.Rows[0]["success"])
    {
        var msg = dt.Rows.Count > 0 ? (string)dt.Rows[0]["message"] : "Error desconocido";
        throw new InvalidOperationException(msg);
    }

    var colaboracionId = (long)dt.Rows[0]["colaboracion_id"];

    // Retornar la colaboración recién creada
    return await _context.Colaboraciones
        .Include(c => c.Miembro).ThenInclude(m => m.Persona)
        .Include(c => c.Detalles).ThenInclude(d => d.TipoColaboracion)
        .FirstAsync(c => c.Id == colaboracionId);
}
```

### 📊 Ganancia estimada

| Métrica | Antes | Después |
|---|---|---|
| Round-trips | 3 (get head + save colaboracion + update head) | 1 |
| Distribución de montos | C# iterando meses×tipos | PL/pgSQL en BD |
| Atomicidad | ❌ Si falla el update del head, colaboracion ya existe | ✅ Todo o nada |

---

## 4. SP-03: `upsert_movimiento_diario`

### 📍 Ubicación actual

| Concepto | Referencia |
|---|---|
| **Archivo** | `RS_system/Services/DiarioFinancieroService.cs` |
| **Métodos** | `GuardarMovimientoAsync` (línea ~148) + `RecalcularTotalesAsync` (línea ~268) |
| **Interfaz** | `IDiarioFinancieroService.GuardarMovimientoAsync` + `RecalcularTotalesAsync` |
| **Modelos** | `DiarioMovimientoInput`, `DiarioFinancieroDetalle`, `DiarioFinancieroCabecera` |
| **Tablas** | `diario_financiero_detalles`, `diario_financiero_cabeceras` |

### 🔴 Problema

- `GuardarMovimientoAsync` hace update campo por campo con `_context.Entry().Property(x => x.XYZ).CurrentValue` — código extremadamente verboso (~30 líneas para setear campos)
- Luego llama a `RecalcularTotalesAsync` que hace **2 SUM queries + 1 UPDATE**
- Total: **3 round-trips** para una operación conceptualmente simple (guardar un movimiento y actualizar totales)

### ✅ Solución propuesta

Un solo SP que hace **UPSERT** del movimiento y recalcula los totales de la cabecera en la misma transacción.

### 📝 SQL del Stored Procedure

```sql
-- ============================================================================
-- SP-03: upsert_movimiento_diario
-- Reemplaza: DiarioFinancieroService.GuardarMovimientoAsync + RecalcularTotalesAsync
-- Descripción: Inserta o actualiza un movimiento del diario financiero
--              y recalcula los totales de la cabecera.
-- ============================================================================

CREATE OR REPLACE FUNCTION upsert_movimiento_diario(
    p_id                    BIGINT,         -- 0 = insert, > 0 = update
    p_cabecera_id           BIGINT,
    p_fecha_movimiento      TIMESTAMP,
    p_tipo                  INTEGER,        -- 1 = Ingreso, 2 = Egreso
    p_categoria_ingreso_id  BIGINT,
    p_categoria_egreso_id   BIGINT,
    p_numero_comprobante    VARCHAR(50),
    p_descripcion           VARCHAR(500),
    p_monto                 NUMERIC(18,2),
    p_metodo_pago_id        BIGINT,
    p_tercero               VARCHAR(200),
    p_observaciones         TEXT,
    p_usuario               VARCHAR(100)
)
RETURNS TABLE(
    success     BOOLEAN,
    message     TEXT,
    detalle_id  BIGINT
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_cabecera      RECORD;
    v_detalle_id    BIGINT;
    v_total_ing     NUMERIC(18,2);
    v_total_egr     NUMERIC(18,2);
BEGIN
    -- 1. Validar cabecera (existe y está abierta)
    SELECT id, estado INTO v_cabecera
    FROM diario_financiero_cabeceras
    WHERE id = p_cabecera_id;

    IF v_cabecera.id IS NULL THEN
        RETURN QUERY SELECT false, 'Cabecera no encontrada', NULL::BIGINT;
        RETURN;
    END IF;

    IF v_cabecera.estado = 'Cerrado' THEN
        RETURN QUERY SELECT false, 'El diario está cerrado', NULL::BIGINT;
        RETURN;
    END IF;

    -- 2. UPSERT del movimiento
    IF p_id > 0 THEN
        -- UPDATE: verificar que el detalle pertenece a esta cabecera
        UPDATE diario_financiero_detalles SET
            fecha_movimiento    = p_fecha_movimiento,
            tipo                = p_tipo,
            categoria_ingreso_id = CASE WHEN p_tipo = 1 THEN p_categoria_ingreso_id ELSE NULL END,
            categoria_egreso_id  = CASE WHEN p_tipo = 2 THEN p_categoria_egreso_id ELSE NULL END,
            numero_comprobante  = p_numero_comprobante,
            descripcion         = p_descripcion,
            monto               = p_monto,
            metodo_pago_id      = p_metodo_pago_id,
            tercero             = p_tercero,
            observaciones       = p_observaciones,
            modificado_por      = p_usuario,
            fecha_modificacion  = NOW()
        WHERE id = p_id AND cabecera_id = p_cabecera_id
        RETURNING id INTO v_detalle_id;

        IF v_detalle_id IS NULL THEN
            RETURN QUERY SELECT false, 'Movimiento no encontrado o no pertenece a esta cabecera', NULL::BIGINT;
            RETURN;
        END IF;
    ELSE
        -- INSERT
        INSERT INTO diario_financiero_detalles (
            cabecera_id, fecha_movimiento, tipo,
            categoria_ingreso_id, categoria_egreso_id,
            numero_comprobante, descripcion, monto,
            metodo_pago_id, tercero, observaciones,
            creado_por, fecha_creacion
        ) VALUES (
            p_cabecera_id, p_fecha_movimiento, p_tipo,
            CASE WHEN p_tipo = 1 THEN p_categoria_ingreso_id ELSE NULL END,
            CASE WHEN p_tipo = 2 THEN p_categoria_egreso_id ELSE NULL END,
            p_numero_comprobante, p_descripcion, p_monto,
            p_metodo_pago_id, p_tercero, p_observaciones,
            p_usuario, NOW()
        )
        RETURNING id INTO v_detalle_id;
    END IF;

    -- 3. Recalcular totales de la cabecera
    SELECT
        COALESCE(SUM(CASE WHEN tipo = 1 THEN monto ELSE 0 END), 0),
        COALESCE(SUM(CASE WHEN tipo = 2 THEN monto ELSE 0 END), 0)
    INTO v_total_ing, v_total_egr
    FROM diario_financiero_detalles
    WHERE cabecera_id = p_cabecera_id;

    UPDATE diario_financiero_cabeceras SET
        total_ingresos = v_total_ing,
        total_egresos  = v_total_egr,
        saldo_dia      = v_total_ing - v_total_egr
    WHERE id = p_cabecera_id;

    RETURN QUERY SELECT true, 'OK', v_detalle_id;
END;
$$;
```

### 🔧 Cambios en C#

**Archivo**: `RS_system/Services/DiarioFinancieroService.cs`

Agregar `IPostgresDirectExecutor` al constructor y reemplazar `GuardarMovimientoAsync`:

```csharp
// Modificar el constructor:
private readonly IPostgresDirectExecutor _pg;

public DiarioFinancieroService(
    ApplicationDbContext context,
    IAccountingIntegrationService accountingIntegration,
    IPostgresDirectExecutor pg)   // ← NUEVO
{
    _context = context;
    _accountingIntegration = accountingIntegration;
    _pg = pg;
}

// Reemplazar TODO el método GuardarMovimientoAsync:
public async Task<DiarioFinancieroDetalle?> GuardarMovimientoAsync(
    DiarioMovimientoInput input, string usuario)
{
    var dt = await _pg.ExecuteStoredProcedureDataTableAsync(
        "upsert_movimiento_diario",
        new NpgsqlParameter("p_id", input.Id),
        new NpgsqlParameter("p_cabecera_id", input.CabeceraId),
        new NpgsqlParameter("p_fecha_movimiento",
            DateTime.SpecifyKind(input.FechaMovimiento, DateTimeKind.Utc)),
        new NpgsqlParameter("p_tipo", input.Tipo),
        new NpgsqlParameter("p_categoria_ingreso_id",
            input.CategoriaIngresoId ?? (object)DBNull.Value),
        new NpgsqlParameter("p_categoria_egreso_id",
            input.CategoriaEgresoId ?? (object)DBNull.Value),
        new NpgsqlParameter("p_numero_comprobante",
            input.NumeroComprobante ?? (object)DBNull.Value),
        new NpgsqlParameter("p_descripcion", input.Descripcion),
        new NpgsqlParameter("p_monto", input.Monto),
        new NpgsqlParameter("p_metodo_pago_id",
            input.MetodoPagoId ?? (object)DBNull.Value),
        new NpgsqlParameter("p_tercero",
            input.Tercero ?? (object)DBNull.Value),
        new NpgsqlParameter("p_observaciones",
            input.Observaciones ?? (object)DBNull.Value),
        new NpgsqlParameter("p_usuario", usuario)
    );

    if (dt.Rows.Count == 0 || !(bool)dt.Rows[0]["success"])
        return null;

    var detalleId = (long)dt.Rows[0]["detalle_id"];

    // Retornar el detalle con sus relaciones
    return await _context.DiarioFinancieroDetalles
        .Include(d => d.CategoriaIngreso)
        .Include(d => d.CategoriaEgreso)
        .Include(d => d.MetodoPago)
        .Include(d => d.Adjuntos)
        .FirstOrDefaultAsync(d => d.Id == detalleId);
}
```

El método `RecalcularTotalesAsync` puede **eliminarse** del servicio y la interfaz, ya que el SP lo hace internamente. También eliminar de `EliminarMovimientoAsync` la llamada a `RecalcularTotalesAsync` y en su lugar llamar al SP con un flag de "solo recalcular".

### 📊 Ganancia estimada

| Métrica | Antes | Después |
|---|---|---|
| Round-trips | 3 (upsert + 2 SUM + update cabecera) | 1 |
| Líneas de código C# | ~80 líneas (update campo a campo) | ~30 líneas |
| Atomicidad | ❌ Totales pueden desincronizarse | ✅ Todo en una transacción |

---

## 5. SP-04: `procesar_cierre_diario`

### 📍 Ubicación actual

| Concepto | Referencia |
|---|---|
| **Archivo** | `RS_system/Services/AccountingIntegrationService.cs` |
| **Método** | `ProcesarCierreDiarioAsync` (línea ~24) |
| **Interfaz** | `IAccountingIntegrationService.ProcesarCierreDiarioAsync` |
| **Modelos** | `PartidaContable`, `DetallePartidaContable`, `DiarioFinancieroCabecera`, `DiarioFinancieroDetalle` |
| **Tablas** | `diario_financiero_cabeceras`, `diario_financiero_detalles`, `partidas_contables`, `detalles_partida_contable`, `categorias_ingreso`, `categorias_egreso`, `cuentas_contables` |

### 🔴 Problema

- Agrupa ingresos y egresos en **C# con LINQ to Objects** (datos ya cargados en memoria)
- Crea 2 partidas contables (ingresos + egresos) — cada una requiere validación + 2 `SaveChangesAsync`
- Las validaciones de cuenta y balance están en `CreatePartidaAsync`, que también hace múltiples queries

### ✅ Solución propuesta

SP que agrupa los movimientos por categoría directamente en SQL, genera las partidas contables en lote y valida todo dentro de la transacción.

### 📝 SQL del Stored Procedure

```sql
-- ============================================================================
-- SP-04: procesar_cierre_diario
-- Reemplaza: AccountingIntegrationService.ProcesarCierreDiarioAsync
-- Descripción: Al cerrar un diario financiero, genera automáticamente
--              las partidas contables de doble entrada para ingresos y egresos.
-- ============================================================================

CREATE OR REPLACE FUNCTION procesar_cierre_diario(
    p_cabecera_id   BIGINT,
    p_usuario       VARCHAR(100)
)
RETURNS TABLE(
    success             BOOLEAN,
    message             TEXT,
    partidas_generadas  INTEGER
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_cabecera          RECORD;
    v_cuenta_caja_id    BIGINT;
    v_periodo_id        BIGINT;
    v_partida_id        BIGINT;
    v_total_ingresos    NUMERIC(18,2);
    v_total_egresos     NUMERIC(18,2);
    v_part_count        INTEGER := 0;
    v_grupo             RECORD;
BEGIN
    -- 1. Cargar cabecera con sus detalles
    SELECT id, fecha, estado, total_ingresos, total_egresos
    INTO v_cabecera
    FROM diario_financiero_cabeceras
    WHERE id = p_cabecera_id;

    IF v_cabecera.id IS NULL THEN
        RETURN QUERY SELECT false, 'Cabecera no encontrada', 0;
        RETURN;
    END IF;

    -- 2. Obtener cuenta caja
    SELECT cc.id INTO v_cuenta_caja_id
    FROM cuentas_contables cc
    WHERE cc.activa = true AND cc.codigo = '1.1.01'
    LIMIT 1;

    IF v_cuenta_caja_id IS NULL THEN
        SELECT cc.id INTO v_cuenta_caja_id
        FROM cuentas_contables cc
        JOIN account_types at ON cc.account_type_id = at.id
        WHERE cc.activa = true AND at.naturaleza = 0
        ORDER BY cc.codigo LIMIT 1;
    END IF;

    IF v_cuenta_caja_id IS NULL THEN
        RETURN QUERY SELECT false, 'No se encontró cuenta de caja', 0;
        RETURN;
    END IF;

    -- 3. Obtener o crear período contable
    SELECT id INTO v_periodo_id
    FROM periodos_contables
    WHERE mes = EXTRACT(MONTH FROM v_cabecera.fecha)
      AND anio = EXTRACT(YEAR FROM v_cabecera.fecha);

    IF v_periodo_id IS NULL THEN
        INSERT INTO periodos_contables (mes, anio, fecha_inicio, fecha_fin, saldo_inicial, fecha_creacion, cerrado)
        VALUES (
            EXTRACT(MONTH FROM v_cabecera.fecha)::INT,
            EXTRACT(YEAR FROM v_cabecera.fecha)::INT,
            DATE_TRUNC('month', v_cabecera.fecha)::DATE,
            (DATE_TRUNC('month', v_cabecera.fecha) + INTERVAL '1 month' - INTERVAL '1 day')::DATE,
            0, NOW(), false
        )
        RETURNING id INTO v_periodo_id;
    END IF;

    -- 4. Contar movimientos
    SELECT
        COALESCE(SUM(CASE WHEN tipo = 1 THEN monto ELSE 0 END), 0),
        COALESCE(SUM(CASE WHEN tipo = 2 THEN monto ELSE 0 END), 0)
    INTO v_total_ingresos, v_total_egresos
    FROM diario_financiero_detalles
    WHERE cabecera_id = p_cabecera_id;

    IF v_total_ingresos = 0 AND v_total_egresos = 0 THEN
        RETURN QUERY SELECT true, 'Sin movimientos para procesar', 0;
        RETURN;
    END IF;

    -- ═══════════════════════════════════════════════════
    -- 5. PARTIDA DE INGRESOS
    -- ═══════════════════════════════════════════════════
    IF v_total_ingresos > 0 THEN
        -- Insertar cabecera de partida
        INSERT INTO partidas_contables (
            fecha, referencia, descripcion, periodo_contable_id, cerrada, fecha_creacion
        ) VALUES (
            v_cabecera.fecha,
            'CIERRE-ING-' || TO_CHAR(v_cabecera.fecha, 'YYYYMMDD'),
            'Cierre de Ingresos Diarios - ' || TO_CHAR(v_cabecera.fecha, 'DD/MM/YYYY'),
            v_periodo_id, false, NOW()
        )
        RETURNING id INTO v_partida_id;

        -- Línea 1: Débito total a Caja
        INSERT INTO detalles_partida_contable (
            partida_contable_id, cuenta_contable_id, debito, credito, descripcion
        ) VALUES (
            v_partida_id, v_cuenta_caja_id, v_total_ingresos, 0,
            'Total Ingresos del Día'
        );

        -- Líneas N: Crédito desglosado por categoría de ingreso
        FOR v_grupo IN
            SELECT
                ci.cuenta_contable_id,
                ci.nombre AS categoria_nombre,
                SUM(dfd.monto) AS total_grupo
            FROM diario_financiero_detalles dfd
            JOIN categorias_ingreso ci ON ci.id = dfd.categoria_ingreso_id
            WHERE dfd.cabecera_id = p_cabecera_id
              AND dfd.tipo = 1 AND dfd.monto > 0
            GROUP BY ci.cuenta_contable_id, ci.nombre
        LOOP
            IF v_grupo.cuenta_contable_id IS NULL THEN
                RETURN QUERY SELECT false,
                    'Categoría de ingreso sin cuenta contable: ' || v_grupo.categoria_nombre,
                    v_part_count;
                RETURN;
            END IF;

            INSERT INTO detalles_partida_contable (
                partida_contable_id, cuenta_contable_id, debito, credito, descripcion
            ) VALUES (
                v_partida_id, v_grupo.cuenta_contable_id, 0, v_grupo.total_grupo,
                'Ingresos por ' || v_grupo.categoria_nombre
            );
        END LOOP;

        v_part_count := v_part_count + 1;
    END IF;

    -- ═══════════════════════════════════════════════════
    -- 6. PARTIDA DE EGRESOS
    -- ═══════════════════════════════════════════════════
    IF v_total_egresos > 0 THEN
        INSERT INTO partidas_contables (
            fecha, referencia, descripcion, periodo_contable_id, cerrada, fecha_creacion
        ) VALUES (
            v_cabecera.fecha,
            'CIERRE-EGR-' || TO_CHAR(v_cabecera.fecha, 'YYYYMMDD'),
            'Cierre de Egresos Diarios - ' || TO_CHAR(v_cabecera.fecha, 'DD/MM/YYYY'),
            v_periodo_id, false, NOW()
        )
        RETURNING id INTO v_partida_id;

        -- Líneas N: Débito desglosado por categoría de egreso
        FOR v_grupo IN
            SELECT
                ce.cuenta_contable_id,
                ce.nombre AS categoria_nombre,
                SUM(dfd.monto) AS total_grupo
            FROM diario_financiero_detalles dfd
            JOIN categorias_egreso ce ON ce.id = dfd.categoria_egreso_id
            WHERE dfd.cabecera_id = p_cabecera_id
              AND dfd.tipo = 2 AND dfd.monto > 0
            GROUP BY ce.cuenta_contable_id, ce.nombre
        LOOP
            IF v_grupo.cuenta_contable_id IS NULL THEN
                RETURN QUERY SELECT false,
                    'Categoría de egreso sin cuenta contable: ' || v_grupo.categoria_nombre,
                    v_part_count;
                RETURN;
            END IF;

            INSERT INTO detalles_partida_contable (
                partida_contable_id, cuenta_contable_id, debito, credito, descripcion
            ) VALUES (
                v_partida_id, v_grupo.cuenta_contable_id, v_grupo.total_grupo, 0,
                'Gastos por ' || v_grupo.categoria_nombre
            );
        END LOOP;

        -- Línea final: Crédito total a Caja
        INSERT INTO detalles_partida_contable (
            partida_contable_id, cuenta_contable_id, debito, credito, descripcion
        ) VALUES (
            v_partida_id, v_cuenta_caja_id, 0, v_total_egresos,
            'Total Egresos del Día'
        );

        v_part_count := v_part_count + 1;
    END IF;

    RETURN QUERY SELECT true, 'Cierre contable procesado', v_part_count;
END;
$$;
```

### 🔧 Cambios en C#

**Archivo**: `RS_system/Services/AccountingIntegrationService.cs`

Agregar `IPostgresDirectExecutor` y reemplazar `ProcesarCierreDiarioAsync`:

```csharp
private readonly IPostgresDirectExecutor _pg;

public AccountingIntegrationService(
    ApplicationDbContext context,
    IContabilidadPartidaDobleService contabilidadService,
    ILogger<AccountingIntegrationService> logger,
    IPostgresDirectExecutor pg)   // ← NUEVO
{
    _context = context;
    _contabilidadService = contabilidadService;
    _logger = logger;
    _pg = pg;
}

public async Task<List<PartidaContable>> ProcesarCierreDiarioAsync(
    long cabeceraId, string usuario)
{
    var dt = await _pg.ExecuteStoredProcedureDataTableAsync(
        "procesar_cierre_diario",
        new NpgsqlParameter("p_cabecera_id", cabeceraId),
        new NpgsqlParameter("p_usuario", usuario)
    );

    if (dt.Rows.Count == 0 || !(bool)dt.Rows[0]["success"])
    {
        var msg = dt.Rows.Count > 0
            ? (string)dt.Rows[0]["message"]
            : "Error desconocido";
        throw new InvalidOperationException(msg);
    }

    _logger.LogInformation("Cierre contable procesado para diario {DiarioId}", cabeceraId);

    // Retornar las partidas generadas (opcional: consultar por referencia)
    var fecha = await _context.DiarioFinancieroCabeceras
        .Where(c => c.Id == cabeceraId)
        .Select(c => c.Fecha)
        .FirstAsync();

    var refIng = $"CIERRE-ING-{fecha:yyyyMMdd}";
    var refEgr = $"CIERRE-EGR-{fecha:yyyyMMdd}";

    return await _context.PartidasContables
        .Include(p => p.Detalles).ThenInclude(d => d.Cuenta)
        .Where(p => p.Referencia == refIng || p.Referencia == refEgr)
        .AsNoTracking()
        .ToListAsync();
}
```

### 📊 Ganancia estimada

| Métrica | Antes | Después |
|---|---|---|
| Round-trips | 5-7 (validaciones + 2×CreatePartida) | 1 |
| Agrupación | C# LINQ to Objects | SQL GROUP BY nativo |
| Validaciones | Múltiples queries en `CreatePartidaAsync` | Dentro del SP |

---

## 6. SP-05: `cerrar_cierre_diezmo`

### 📍 Ubicación actual

| Concepto | Referencia |
|---|---|
| **Archivos** | `RS_system/Services/DiezmoCierreService.cs` + `RS_system/Services/DiezmoCalculoService.cs` |
| **Métodos** | `CerrarCierreAsync` (línea ~246), `RecalcularTotales` de DiezmoCalculoService |
| **Interfaz** | `IDiezmoCierreService.CerrarCierreAsync` |
| **Modelos** | `DiezmoCierre` |
| **Tablas** | `diezmo_cierres`, `diezmo_detalles`, `diezmo_salidas`, `diezmo_bitacora` |

### 🔴 Problema

- `DiezmoCalculoService.RecalcularTotales` recibe el objeto DiezmoCierre completo (con Includes de detalles y salidas), itera las colecciones en C# con LINQ (`Sum`, `Where`), y recalcula 5 campos
- `CerrarCierreAsync` hace query separado + update + insert de bitácora
- Total: 3-4 round-trips

### ✅ Solución propuesta

SP único que recalcula totales con aggregates SQL nativos, marca como cerrado, e inserta la bitácora.

### 📝 SQL del Stored Procedure

```sql
-- ============================================================================
-- SP-05: cerrar_cierre_diezmo
-- Reemplaza: DiezmoCierreService.CerrarCierreAsync + DiezmoCalculoService.RecalcularTotales
-- Descripción: Recalcula totales del cierre de diezmo usando aggregates SQL,
--              lo marca como cerrado y registra en bitácora.
-- ============================================================================

CREATE OR REPLACE FUNCTION cerrar_cierre_diezmo(
    p_cierre_id BIGINT,
    p_usuario   VARCHAR(100)
)
RETURNS TABLE(
    success         BOOLEAN,
    message         TEXT,
    total_recibido  NUMERIC(12,2),
    total_neto      NUMERIC(12,2),
    total_salidas   NUMERIC(12,2),
    saldo_final     NUMERIC(12,2)
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_cierre        RECORD;
    v_total_rec     NUMERIC(12,2);
    v_total_cambio  NUMERIC(12,2);
    v_total_neto    NUMERIC(12,2);
    v_total_sal     NUMERIC(12,2);
    v_saldo_final   NUMERIC(12,2);
BEGIN
    -- 1. Validar que el cierre existe y no está eliminado
    SELECT * INTO v_cierre
    FROM diezmo_cierres
    WHERE id = p_cierre_id AND eliminado = false;

    IF v_cierre.id IS NULL THEN
        RETURN QUERY SELECT false, 'Cierre no encontrado', 0, 0, 0, 0;
        RETURN;
    END IF;

    IF v_cierre.cerrado THEN
        RETURN QUERY SELECT false, 'El cierre ya está cerrado', 0, 0, 0, 0;
        RETURN;
    END IF;

    -- 2. Recalcular totales con aggregates SQL (no LINQ en memoria)
    SELECT
        COALESCE(SUM(monto_entregado), 0),
        COALESCE(SUM(cambio_entregado), 0),
        COALESCE(SUM(monto_neto), 0)
    INTO v_total_rec, v_total_cambio, v_total_neto
    FROM diezmo_detalles
    WHERE diezmo_cierre_id = p_cierre_id AND eliminado = false;

    SELECT COALESCE(SUM(monto), 0)
    INTO v_total_sal
    FROM diezmo_salidas
    WHERE diezmo_cierre_id = p_cierre_id AND eliminado = false;

    v_saldo_final := v_total_neto - v_total_sal;

    -- 3. Actualizar cierre con totales recalculados y marcarlo como cerrado
    UPDATE diezmo_cierres SET
        total_recibido  = v_total_rec,
        total_cambio    = v_total_cambio,
        total_neto      = v_total_neto,
        total_salidas   = v_total_sal,
        saldo_final     = v_saldo_final,
        cerrado         = true,
        fecha_cierre    = NOW(),
        cerrado_por     = p_usuario,
        actualizado_en  = NOW(),
        actualizado_por = p_usuario
    WHERE id = p_cierre_id;

    -- 4. Registrar en bitácora
    INSERT INTO diezmo_bitacora (diezmo_cierre_id, accion, detalle, realizado_por, realizado_en)
    VALUES (p_cierre_id, 'CIERRE',
        'Cierre sellado. Saldo final: ' || TO_CHAR(v_saldo_final, 'FM999,999,990.00'),
        p_usuario, NOW());

    RETURN QUERY SELECT true, 'Cierre procesado exitosamente',
        v_total_rec, v_total_neto, v_total_sal, v_saldo_final;
END;
$$;
```

### 🔧 Cambios en C#

**Archivo**: `RS_system/Services/DiezmoCierreService.cs`

Agregar `IPostgresDirectExecutor` y reemplazar `CerrarCierreAsync`:

```csharp
private readonly IPostgresDirectExecutor _pg;

public DiezmoCierreService(
    ApplicationDbContext context,
    IDiezmoCalculoService calculo,
    IPostgresDirectExecutor pg)
{
    _context = context;
    _calculo = calculo;
    _pg = pg;
}

public async Task CerrarCierreAsync(long cierreId, string usuario)
{
    var dt = await _pg.ExecuteStoredProcedureDataTableAsync(
        "cerrar_cierre_diezmo",
        new NpgsqlParameter("p_cierre_id", cierreId),
        new NpgsqlParameter("p_usuario", usuario)
    );

    if (dt.Rows.Count == 0 || !(bool)dt.Rows[0]["success"])
    {
        var msg = dt.Rows.Count > 0
            ? (string)dt.Rows[0]["message"]
            : "Error al cerrar";
        throw new InvalidOperationException(msg);
    }
}
```

**Nota**: El método `RecalcularCierreAsync` también puede simplificarse llamando a una variante del SP que no marque como cerrado. El `DiezmoCalculoService.RecalcularTotales` (en memoria) queda obsoleto para estas operaciones.

### 📊 Ganancia estimada

| Métrica | Antes | Después |
|---|---|---|
| Round-trips | 3-4 (query cierre + recalc en C# + update + insert bitácora) | 1 |
| Cálculo de totales | LINQ to Objects iterando colecciones | SUM() nativo en PostgreSQL |

---

## 7. FN-06: `fn_balance_general_recursivo`

### 📍 Ubicación actual

| Concepto | Referencia |
|---|---|
| **Archivo** | `RS_system/Services/ContabilidadPartidaDobleService.cs` |
| **Método** | `GetBalanceGeneralRecursivoAsync` (línea ~340) + `GetNivel` + `AjustarNaturalezaRecursivo` |
| **Interfaz** | `IContabilidadPartidaDobleService.GetBalanceGeneralRecursivoAsync` |
| **Modelos** | `FinancialReportViewModel`, `AccountReportItemViewModel`, `ReportSectionViewModel` |
| **Tablas** | `cuentas_contables`, `detalles_partida_contable`, `partidas_contables`, `account_types` |

### 🔴 Problema

- Carga **TODAS** las cuentas y **TODOS** los saldos a memoria
- Calcula la profundidad de cada cuenta con función recursiva en C# (`GetNivel`)
- Propaga saldos de hijos a padres iterando en C# (`AjustarNaturalezaRecursivo`)
- Caso perfecto para **Recursive CTE** de PostgreSQL

### 📝 SQL de la Función

```sql
-- ============================================================================
-- FN-06: fn_balance_general_recursivo
-- Reemplaza: ContabilidadPartidaDobleService.GetBalanceGeneralRecursivoAsync
-- Descripción: Calcula el Balance General con propagación recursiva de saldos
--              usando CTE recursivo de PostgreSQL.
-- ============================================================================

CREATE OR REPLACE FUNCTION fn_balance_general_recursivo(
    p_fecha_corte DATE
)
RETURNS TABLE(
    account_type_id     INTEGER,
    account_type_nombre VARCHAR(100),
    naturaleza          INTEGER,            -- 0 = Deudora, 1 = Acreedora
    orden               INTEGER,
    cuenta_id           BIGINT,
    cuenta_codigo       VARCHAR(20),
    cuenta_nombre       VARCHAR(200),
    padre_id            BIGINT,
    nivel               INTEGER,
    saldo_bruto         NUMERIC(18,2),
    saldo_neto          NUMERIC(18,2)       -- Ajustado por naturaleza
)
LANGUAGE sql
STABLE
AS $$
    WITH RECURSIVE
    -- Paso 1: Saldos base de cada cuenta (solo cuentas hoja con movimientos)
    saldos_base AS (
        SELECT
            d.cuenta_contable_id,
            SUM(d.debito - d.credito) AS saldo
        FROM detalles_partida_contable d
        JOIN partidas_contables p ON p.id = d.partida_contable_id
        WHERE p.fecha::DATE <= p_fecha_corte
        GROUP BY d.cuenta_contable_id
    ),
    -- Paso 2: Árbol jerárquico completo con CTE recursivo
    arbol_cuentas AS (
        -- Raíces (cuentas sin padre)
        SELECT
            cc.id,
            cc.codigo,
            cc.nombre,
            cc.padre_id,
            cc.account_type_id,
            0 AS nivel,
            COALESCE(sb.saldo, 0) AS saldo_propio
        FROM cuentas_contables cc
        LEFT JOIN saldos_base sb ON sb.cuenta_contable_id = cc.id
        WHERE cc.padre_id IS NULL AND cc.activa = true

        UNION ALL

        -- Hijos (recursivo)
        SELECT
            cc.id,
            cc.codigo,
            cc.nombre,
            cc.padre_id,
            cc.account_type_id,
            ac.nivel + 1,
            COALESCE(sb.saldo, 0)
        FROM cuentas_contables cc
        JOIN arbol_cuentas ac ON cc.padre_id = ac.id
        LEFT JOIN saldos_base sb ON sb.cuenta_contable_id = cc.id
        WHERE cc.activa = true
    ),
    -- Paso 3: Propagar saldos de hojas a padres (agregación bottom-up)
    saldos_propagados AS (
        SELECT
            ac.id,
            ac.codigo,
            ac.nombre,
            ac.padre_id,
            ac.account_type_id,
            ac.nivel,
            -- Saldo propio + suma de saldos de todos los descendientes
            ac.saldo_propio + COALESCE(
                (SELECT SUM(ac2.saldo_propio)
                 FROM arbol_cuentas ac2
                 WHERE ac2.id IN (
                     WITH RECURSIVE hijos AS (
                         SELECT id FROM cuentas_contables WHERE padre_id = ac.id
                         UNION ALL
                         SELECT cc.id FROM cuentas_contables cc JOIN hijos h ON cc.padre_id = h.id
                     )
                     SELECT id FROM hijos
                 )
                ), 0
            ) AS saldo_bruto
        FROM arbol_cuentas ac
    )
    SELECT
        at.id,
        at.nombre,
        at.naturaleza,
        at.orden,
        sp.id,
        sp.codigo,
        sp.nombre,
        sp.padre_id,
        sp.nivel,
        sp.saldo_bruto,
        -- Ajuste por naturaleza: Acreedora = invertir signo
        CASE WHEN at.naturaleza = 1 THEN -sp.saldo_bruto ELSE sp.saldo_bruto END AS saldo_neto
    FROM saldos_propagados sp
    JOIN account_types at ON at.id = sp.account_type_id
    WHERE at.activo = true AND at.categoria_reporte = 0  -- 0 = Balance
    ORDER BY at.orden, sp.codigo;
$$;
```

### 🔧 Cambios en C#

```csharp
// Reemplazar GetBalanceGeneralRecursivoAsync:
public async Task<FinancialReportViewModel> GetBalanceGeneralRecursivoAsync(DateTime fechaCorte)
{
    var rows = await _postgresExecutor.ExecuteStoredProcedureListAsync(
        "fn_balance_general_recursivo",  -- Nota: se llama como SELECT * FROM fn_...
        reader => new
        {
            AccountTypeId = reader.GetInt32(0),
            AccountTypeNombre = reader.GetString(1),
            Naturaleza = reader.GetInt32(2),
            Orden = reader.GetInt32(3),
            CuentaId = reader.GetInt64(4),
            CuentaCodigo = reader.GetString(5),
            CuentaNombre = reader.GetString(6),
            PadreId = reader.IsDBNull(7) ? (long?)null : reader.GetInt64(7),
            Nivel = reader.GetInt32(8),
            SaldoBruto = reader.GetDecimal(9),
            SaldoNeto = reader.GetDecimal(10)
        },
        new NpgsqlParameter("p_fecha_corte", fechaCorte.Date)
    );

    // Agrupar resultados en el ViewModel (ligero, ya que los datos vienen calculados)
    var report = new FinancialReportViewModel { Titulo = "Balance General", FechaFin = fechaCorte };
    // ... mapear rows a ReportSectionViewModel y AccountReportItemViewModel ...
    return report;
}
```

> ⚠️ **Nota técnica**: Las funciones SQL se llaman con `SELECT * FROM fn_...()` en lugar de `CommandType.StoredProcedure`. Puedes usar `ExecuteQueryListAsync` del `PostgresDirectExecutor` o `ExecuteStoredProcedureListAsync` con una consulta `SELECT * FROM fn_name(@p1)`.

### 📊 Ganancia estimada

| Métrica | Antes | Después |
|---|---|---|
| Datos transferidos | Todas las cuentas + todos los detalles del período | Solo resultados finales |
| Cálculo recursivo | C# (GetNivel recursivo) | PostgreSQL Recursive CTE |
| Memoria C# | Alta (diccionarios + listas anidadas) | Mínima |

---

## 8. FN-07: `fn_balance_general` + `fn_estado_resultados`

### 📍 Ubicación actual

| Concepto | Referencia |
|---|---|
| **Archivo** | `RS_system/Services/ContabilidadPartidaDobleService.cs` |
| **Métodos** | `GetBalanceGeneralAsync` (línea ~307), `GetEstadoResultadosAsync` (línea ~419) |
| **Interfaz** | `IContabilidadPartidaDobleService.GetBalanceGeneralAsync`, `GetEstadoResultadosAsync` |

### 🔴 Problema

Ambos métodos siguen el mismo patrón: cargar tipos de cuenta → cargar saldos agrupados → procesar en memoria. Son consultas de solo lectura, ideales para funciones SQL.

### 📝 SQL de las Funciones

```sql
-- ============================================================================
-- FN-07a: fn_balance_general
-- Reemplaza: ContabilidadPartidaDobleService.GetBalanceGeneralAsync
-- Descripción: Balance General a fecha de corte (versión plana, no recursiva).
-- ============================================================================

CREATE OR REPLACE FUNCTION fn_balance_general(
    p_fecha_corte DATE
)
RETURNS TABLE(
    seccion_nombre      VARCHAR(100),
    naturaleza          INTEGER,
    seccion_orden       INTEGER,
    cuenta_id           BIGINT,
    cuenta_codigo       VARCHAR(20),
    cuenta_nombre       VARCHAR(200),
    saldo               NUMERIC(18,2),
    seccion_total       NUMERIC(18,2)
)
LANGUAGE sql
STABLE
AS $$
    WITH saldos AS (
        SELECT
            d.cuenta_contable_id,
            d.cuenta.account_type_id,
            SUM(d.debito - d.credito) AS saldo_bruto
        FROM detalles_partida_contable d
        JOIN partidas_contables p ON p.id = d.partida_contable_id
        JOIN cuentas_contables cc ON cc.id = d.cuenta_contable_id
        WHERE p.fecha::DATE <= p_fecha_corte
        GROUP BY d.cuenta_contable_id, d.cuenta.account_type_id
    ),
    secciones AS (
        SELECT
            at.id           AS tipo_id,
            at.nombre       AS seccion_nombre,
            at.naturaleza   AS naturaleza,
            at.orden        AS seccion_orden,
            cc.id           AS cuenta_id,
            cc.codigo       AS cuenta_codigo,
            cc.nombre       AS cuenta_nombre,
            CASE WHEN at.naturaleza = 1
                THEN -COALESCE(s.saldo_bruto, 0)
                ELSE COALESCE(s.saldo_bruto, 0)
            END AS saldo
        FROM account_types at
        JOIN cuentas_contables cc ON cc.account_type_id = at.id
        LEFT JOIN saldos s ON s.cuenta_contable_id = cc.id
        WHERE at.activo = true
          AND at.categoria_reporte = 0  -- Balance
          AND cc.activa = true
    )
    SELECT
        seccion_nombre,
        naturaleza,
        seccion_orden,
        cuenta_id,
        cuenta_codigo,
        cuenta_nombre,
        saldo,
        SUM(saldo) OVER (PARTITION BY tipo_id) AS seccion_total
    FROM secciones
    WHERE ABS(saldo) > 0.001
    ORDER BY seccion_orden, cuenta_codigo;
$$;


-- ============================================================================
-- FN-07b: fn_estado_resultados
-- Reemplaza: ContabilidadPartidaDobleService.GetEstadoResultadosAsync
-- Descripción: Estado de Resultados (Pérdidas y Ganancias) para un mes/año.
-- ============================================================================

CREATE OR REPLACE FUNCTION fn_estado_resultados(
    p_mes  INTEGER,
    p_anio INTEGER
)
RETURNS TABLE(
    seccion_nombre      VARCHAR(100),
    naturaleza          INTEGER,
    seccion_orden       INTEGER,
    cuenta_id           BIGINT,
    cuenta_codigo       VARCHAR(20),
    cuenta_nombre       VARCHAR(200),
    saldo               NUMERIC(18,2),
    seccion_total       NUMERIC(18,2)
)
LANGUAGE sql
STABLE
AS $$
    WITH fecha_rango AS (
        SELECT
            MAKE_DATE(p_anio, p_mes, 1) AS inicio,
            (MAKE_DATE(p_anio, p_mes, 1) + INTERVAL '1 month')::DATE AS fin
    ),
    saldos AS (
        SELECT
            d.cuenta_contable_id,
            cc.account_type_id,
            SUM(d.debito) AS total_debito,
            SUM(d.credito) AS total_credito
        FROM detalles_partida_contable d
        JOIN partidas_contables p ON p.id = d.partida_contable_id
        JOIN cuentas_contables cc ON cc.id = d.cuenta_contable_id
        CROSS JOIN fecha_rango fr
        WHERE p.fecha::DATE >= fr.inicio
          AND p.fecha::DATE < fr.fin
        GROUP BY d.cuenta_contable_id, cc.account_type_id
    ),
    secciones AS (
        SELECT
            at.id           AS tipo_id,
            at.nombre       AS seccion_nombre,
            at.naturaleza   AS naturaleza,
            at.orden        AS seccion_orden,
            cc.id           AS cuenta_id,
            cc.codigo       AS cuenta_codigo,
            cc.nombre       AS cuenta_nombre,
            CASE WHEN at.naturaleza = 1
                THEN -COALESCE(s.total_debito - s.total_credito, 0)
                ELSE COALESCE(s.total_debito - s.total_credito, 0)
            END AS saldo
        FROM account_types at
        JOIN cuentas_contables cc ON cc.account_type_id = at.id
        LEFT JOIN saldos s ON s.cuenta_contable_id = cc.id
        WHERE at.activo = true
          AND at.categoria_reporte = 1  -- Resultado
          AND cc.activa = true
    )
    SELECT
        seccion_nombre,
        naturaleza,
        seccion_orden,
        cuenta_id,
        cuenta_codigo,
        cuenta_nombre,
        saldo,
        SUM(saldo) OVER (PARTITION BY tipo_id) AS seccion_total
    FROM secciones
    WHERE ABS(saldo) > 0.001
    ORDER BY seccion_orden, cuenta_codigo;
$$;
```

### 🔧 Cambios en C#

El patrón es el mismo que FN-06: llamar con `SELECT * FROM fn_balance_general(@fecha)` y mapear resultados a los ViewModels `BalanceGeneralResult` / `EstadoResultadosResult`.

### 📊 Ganancia estimada

| Métrica | Antes | Después |
|---|---|---|
| Queries por reporte | 2-3 (tipos + saldos + posiblemente cuentas) | 1 |
| Datos transferidos | Todas las cuentas + todos los movimientos | Solo el resultado agregado |

---

## 9. FN-08: `fn_reporte_colaboraciones` + `fn_estado_cuenta_miembro`

### 📍 Ubicación actual

| Concepto | Referencia |
|---|---|
| **Archivo** | `RS_system/Services/ColaboracionService.cs` |
| **Métodos** | `GenerarReportePorFechasAsync` (línea ~269), `GenerarEstadoCuentaAsync` (línea ~341) |

### 🔴 Problema

- `GenerarReportePorFechasAsync`: carga todas las colaboraciones con múltiples Includes, agrupa en memoria con LINQ
- `GenerarEstadoCuentaAsync`: 2 queries separadas + agrupación anidada en C# con `SelectMany` + `GroupBy`
- Para períodos largos o miembros con muchas colaboraciones, la carga de datos es innecesariamente alta

### 📝 SQL de las Funciones

```sql
-- ============================================================================
-- FN-08a: fn_reporte_colaboraciones
-- Reemplaza: ColaboracionService.GenerarReportePorFechasAsync
-- Descripción: Reporte de colaboraciones en un rango de fechas con desglose
--              por tipo de colaboración y detalle de movimientos.
-- ============================================================================

CREATE OR REPLACE FUNCTION fn_reporte_colaboraciones(
    p_fecha_inicio  TIMESTAMP,
    p_fecha_fin     TIMESTAMP
)
RETURNS TABLE(
    -- Totales generales
    total_recaudado         NUMERIC(12,2),
    -- Desglose por tipo
    tipo_nombre             VARCHAR(100),
    tipo_cantidad_meses     BIGINT,
    tipo_total              NUMERIC(12,2),
    -- Detalle de movimientos
    colaboracion_id         BIGINT,
    movimiento_fecha        TIMESTAMP,
    nombre_miembro          TEXT,
    tipos_colaboracion      TEXT,
    periodo_cubierto        TEXT,
    movimiento_monto        NUMERIC(12,2)
)
LANGUAGE sql
STABLE
AS $$
    WITH colaboraciones_filtradas AS (
        SELECT
            c.id,
            c.fecha_registro,
            c.monto_total,
            p.nombres || ' ' || p.apellidos AS nombre_miembro
        FROM colaboraciones c
        JOIN miembros m ON m.id = c.miembro_id
        JOIN personas p ON p.id = m.persona_id
        WHERE c.fecha_registro >= p_fecha_inicio
          AND c.fecha_registro <= p_fecha_fin
    ),
    desglose AS (
        SELECT
            tc.nombre AS tipo_nombre,
            COUNT(*) AS cantidad_meses,
            SUM(dc.monto) AS total_tipo
        FROM detalle_colaboraciones dc
        JOIN colaboraciones_filtradas cf ON cf.id = dc.colaboracion_id
        JOIN tipos_colaboracion tc ON tc.id = dc.tipo_colaboracion_id
        GROUP BY tc.nombre
    ),
    movimientos AS (
        SELECT
            cf.id AS colaboracion_id,
            cf.fecha_registro,
            cf.nombre_miembro,
            cf.monto_total,
            STRING_AGG(DISTINCT tc.nombre, ', ' ORDER BY tc.nombre) AS tipos_colab
        FROM colaboraciones_filtradas cf
        JOIN detalle_colaboraciones dc ON dc.colaboracion_id = cf.id
        JOIN tipos_colaboracion tc ON tc.id = dc.tipo_colaboracion_id
        GROUP BY cf.id, cf.fecha_registro, cf.nombre_miembro, cf.monto_total
    ),
    total AS (
        SELECT COALESCE(SUM(monto_total), 0) AS total_rec
        FROM colaboraciones_filtradas
    )
    SELECT
        t.total_rec,
        d.tipo_nombre,
        d.cantidad_meses,
        d.total_tipo,
        m.colaboracion_id,
        m.fecha_registro,
        m.nombre_miembro,
        m.tipos_colab,
        '',  -- periodo_cubierto (se puede calcular en SQL si se necesita)
        m.monto_total
    FROM total t
    CROSS JOIN desglose d
    CROSS JOIN movimientos m
    ORDER BY d.tipo_nombre, m.fecha_registro DESC;
$$;


-- ============================================================================
-- FN-08b: fn_estado_cuenta_miembro
-- Reemplaza: ColaboracionService.GenerarEstadoCuentaAsync
-- Descripción: Estado de cuenta de un miembro con historial por tipo de colaboración.
-- ============================================================================

CREATE OR REPLACE FUNCTION fn_estado_cuenta_miembro(
    p_miembro_id BIGINT
)
RETURNS TABLE(
    miembro_id          BIGINT,
    nombre_miembro      TEXT,
    total_aportado      NUMERIC(12,2),
    tipo_nombre         VARCHAR(100),
    tipo_total          NUMERIC(12,2),
    registro_mes        INTEGER,
    registro_anio       INTEGER,
    registro_monto      NUMERIC(12,2),
    registro_fecha      TIMESTAMP
)
LANGUAGE sql
STABLE
AS $$
    WITH datos AS (
        SELECT
            m.id AS miembro_id,
            p.nombres || ' ' || p.apellidos AS nombre_miembro,
            tc.nombre AS tipo_nombre,
            dc.mes,
            dc.anio,
            dc.monto,
            c.fecha_registro
        FROM miembros m
        JOIN personas p ON p.id = m.persona_id
        JOIN colaboraciones c ON c.miembro_id = m.id
        JOIN detalle_colaboraciones dc ON dc.colaboracion_id = c.id
        JOIN tipos_colaboracion tc ON tc.id = dc.tipo_colaboracion_id
        WHERE m.id = p_miembro_id
    ),
    totales AS (
        SELECT
            COALESCE(SUM(monto), 0) AS total_ap
        FROM datos
    ),
    por_tipo AS (
        SELECT
            tipo_nombre,
            SUM(monto) AS total_tipo
        FROM datos
        GROUP BY tipo_nombre
    )
    SELECT
        d.miembro_id,
        d.nombre_miembro,
        t.total_ap,
        pt.tipo_nombre,
        pt.total_tipo,
        d.mes,
        d.anio,
        d.monto,
        d.fecha_registro
    FROM datos d
    CROSS JOIN totales t
    JOIN por_tipo pt ON pt.tipo_nombre = d.tipo_nombre
    ORDER BY pt.tipo_nombre, d.anio, d.mes;
$$;
```

### 📊 Ganancia estimada

| Métrica | Antes | Después |
|---|---|---|
| Queries por reporte | 1 pesada (con múltiples Includes) | 1 función SQL |
| Agrupación | LINQ to Objects en C# | SQL GROUP BY nativo |
| Escalabilidad | Degrada con muchos datos | Índices de BD aprovechados |

---

## 10. Estrategia de Migración

### 10.1 Orden de implementación recomendado

| Fase | SPs | Justificación |
|---|---|---|
| **Fase A** (semana 1) | SP-01 `guardar_movimientos_bulk` | Mayor impacto (4+N → 1 query). Ya usa SPs parcialmente. |
| **Fase B** (semana 2) | SP-03 `upsert_movimiento_diario` | Corrige el patrón más verboso. Bajo riesgo. |
| **Fase C** (semana 3) | SP-04 `procesar_cierre_diario` | Elimina duplicación de lógica de agrupación. |
| **Fase D** (semana 4) | SP-02 `registrar_colaboracion` + SP-05 `cerrar_cierre_diezmo` | Completa operaciones transaccionales restantes. |
| **Fase E** (semana 5-6) | FN-06, FN-07, FN-08 (funciones de reportes) | Solo lectura, sin riesgo de corrupción de datos. |

### 10.2 Proceso de migración para cada SP

1. **Crear el SP en BD** con prefijo `sp_v2_` (para testing en paralelo)
2. **Ejecutar pruebas** con datos reales comparando resultados SP vs C#
3. **Agregar feature flag** en `appsettings.json`: `"UseStoredProcedures": { "GuardarMovimientosBulk": true }`
4. **Código C#** con bifurcación:

```csharp
public async Task<bool> GuardarMovimientosBulkAsync(long reporteId, List<MovimientoGeneral> movimientos)
{
    if (_configuration.GetValue<bool>("UseStoredProcedures:GuardarMovimientosBulk"))
        return await GuardarMovimientosBulkViaSPAsync(reporteId, movimientos);
    else
        return await GuardarMovimientosBulkLegacyAsync(reporteId, movimientos);
}
```

5. **Validar en staging** durante 1-2 semanas
6. **Eliminar código legacy** y el feature flag

### 10.3 Pruebas recomendadas

| Tipo | Descripción |
|---|---|
| **Unitarias** | Probar el SP con datos de prueba conocidos, verificar sumas y totales |
| **Integración** | Ejecutar la operación completa desde el controller y verificar el estado final de la BD |
| **Regresión** | Asegurar que otras operaciones (consultas, reportes) no se rompen |
| **Performance** | Medir tiempos antes/después con volúmenes reales de datos |

### 10.4 Rollback

Cada SP puede revertirse simplemente desactivando el feature flag. El código legacy se mantiene hasta que la validación sea completa.

---

## Apéndice A: Archivos que requieren cambios

| Archivo | Cambio requerido |
|---|---|
| `Services/ContabilidadGeneralService.cs` | Constructor ya tiene `IPostgresDirectExecutor`. Reemplazar `GuardarMovimientosBulkAsync`. |
| `Services/ColaboracionService.cs` | Agregar `IPostgresDirectExecutor` al constructor. Reemplazar `RegistrarColaboracionAsync`. |
| `Services/DiarioFinancieroService.cs` | Agregar `IPostgresDirectExecutor` al constructor. Reemplazar `GuardarMovimientoAsync`. Eliminar `RecalcularTotalesAsync`. |
| `Services/AccountingIntegrationService.cs` | Agregar `IPostgresDirectExecutor` al constructor. Reemplazar `ProcesarCierreDiarioAsync`. |
| `Services/DiezmoCierreService.cs` | Agregar `IPostgresDirectExecutor` al constructor. Reemplazar `CerrarCierreAsync`. |
| `Services/ContabilidadPartidaDobleService.cs` | Reemplazar métodos de reportes financieros. Llamar funciones SQL. |
| `Services/IColaboracionService.cs` | Sin cambios (misma firma). |
| `Services/IDiarioFinancieroService.cs` | Eliminar `RecalcularTotalesAsync` de la interfaz. |
| `Services/IDiezmoCierreService.cs` | Sin cambios (misma firma). |

## Apéndice B: Script SQL completo de despliegue

Todos los SPs arriba deben ejecutarse en orden. Se recomienda crear un archivo `migracion_stored_procedures.sql` en la raíz del proyecto y ejecutarlo con:

```bash
psql -h localhost -U postgres -d rs_system -f migracion_stored_procedures.sql
```
