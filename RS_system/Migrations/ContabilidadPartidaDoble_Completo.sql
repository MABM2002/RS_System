-- ============================================================================
-- SCRIPT COMPLETO: Módulo de Contabilidad por Partida Doble
-- PostgreSQL
-- ============================================================================
-- Este script crea las tablas, índices, datos semilla y migración histórica
-- para implementar el sistema de contabilidad por partida doble.
-- ============================================================================
-- Fecha: 2026-05-23
-- ============================================================================

BEGIN;

-- ============================================================================
-- PARTE 1: CREACIÓN DE TABLAS NUEVAS
-- ============================================================================

-- 1.1 Catálogo de Cuentas Contables
CREATE TABLE IF NOT EXISTS cuentas_contables (
    id              BIGSERIAL       PRIMARY KEY,
    codigo          VARCHAR(20)     NOT NULL,
    nombre          VARCHAR(150)    NOT NULL,
    padre_id        BIGINT          REFERENCES cuentas_contables(id) ON DELETE RESTRICT,
    tipo            INTEGER         NOT NULL CHECK (tipo IN (1, 2, 3, 4, 5)),
    -- 1=Activo, 2=Pasivo, 3=Capital, 4=Ingreso, 5=Gasto
    activa          BOOLEAN         NOT NULL DEFAULT TRUE,
    fecha_creacion  TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_cuentas_contables_codigo ON cuentas_contables(codigo);
CREATE INDEX IF NOT EXISTS ix_cuentas_contables_padre_id ON cuentas_contables(padre_id);
CREATE INDEX IF NOT EXISTS ix_cuentas_contables_tipo ON cuentas_contables(tipo);

COMMENT ON TABLE cuentas_contables IS 'Catálogo de cuentas contables con estructura jerárquica padre-hijo';
COMMENT ON COLUMN cuentas_contables.codigo IS 'Código jerárquico (ej. "1.1.01", "4.1.02")';
COMMENT ON COLUMN cuentas_contables.tipo IS 'Tipo de cuenta: 1=Activo, 2=Pasivo, 3=Capital, 4=Ingreso, 5=Gasto';

-- 1.2 Períodos Contables
CREATE TABLE IF NOT EXISTS periodos_contables (
    id              BIGSERIAL       PRIMARY KEY,
    mes             INTEGER         NOT NULL CHECK (mes >= 1 AND mes <= 12),
    anio            INTEGER         NOT NULL,
    fecha_inicio    TIMESTAMPTZ     NOT NULL,
    fecha_fin       TIMESTAMPTZ     NOT NULL,
    cerrado         BOOLEAN         NOT NULL DEFAULT FALSE,
    saldo_inicial   DECIMAL(18,2)   NOT NULL DEFAULT 0,
    fecha_creacion  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    fecha_cierre    TIMESTAMPTZ,
    cerrado_por     VARCHAR(100)
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_periodos_contables_mes_anio ON periodos_contables(mes, anio);

COMMENT ON TABLE periodos_contables IS 'Período contable mensual. Controla el cierre y bloqueo de ediciones';

-- 1.3 Partidas Contables (Asientos)
CREATE TABLE IF NOT EXISTS partidas_contables (
    id                      BIGSERIAL       PRIMARY KEY,
    fecha                   DATE            NOT NULL,
    referencia              VARCHAR(50),
    descripcion             VARCHAR(500),
    periodo_contable_id     BIGINT          REFERENCES periodos_contables(id) ON DELETE RESTRICT,
    cerrada                 BOOLEAN         NOT NULL DEFAULT FALSE,
    movimiento_general_id   BIGINT          REFERENCES movimientos_generales(id) ON DELETE SET NULL,
    contabilidad_registro_id BIGINT         REFERENCES contabilidad_registros(id) ON DELETE SET NULL,
    fecha_creacion          TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_partidas_contables_periodo ON partidas_contables(periodo_contable_id);
CREATE INDEX IF NOT EXISTS ix_partidas_contables_fecha ON partidas_contables(fecha);
CREATE INDEX IF NOT EXISTS ix_partidas_contables_movimiento_general ON partidas_contables(movimiento_general_id);
CREATE INDEX IF NOT EXISTS ix_partidas_contables_contabilidad_registro ON partidas_contables(contabilidad_registro_id);

COMMENT ON TABLE partidas_contables IS 'Encabezado de asiento contable (Partida de Diario)';
COMMENT ON COLUMN partidas_contables.referencia IS 'Número de referencia o comprobante (ej. "AS-2026-0001")';
COMMENT ON COLUMN partidas_contables.cerrada IS 'Indica si esta partida está bloqueada (período cerrado)';

-- 1.4 Detalles de Partidas Contables (Líneas Débito/Crédito)
CREATE TABLE IF NOT EXISTS detalles_partida_contable (
    id                  BIGSERIAL       PRIMARY KEY,
    partida_contable_id BIGINT          NOT NULL REFERENCES partidas_contables(id) ON DELETE CASCADE,
    cuenta_contable_id  BIGINT          NOT NULL REFERENCES cuentas_contables(id) ON DELETE RESTRICT,
    debito              DECIMAL(18,2)   NOT NULL DEFAULT 0 CHECK (debito >= 0),
    credito             DECIMAL(18,2)   NOT NULL DEFAULT 0 CHECK (credito >= 0),
    descripcion         VARCHAR(300),
    CONSTRAINT ck_detalle_no_ambos CHECK (NOT (debito > 0 AND credito > 0)),
    CONSTRAINT ck_detalle_algun_monto CHECK (debito > 0 OR credito > 0)
);

CREATE INDEX IF NOT EXISTS ix_detalles_partida_contable_partida ON detalles_partida_contable(partida_contable_id);
CREATE INDEX IF NOT EXISTS ix_detalles_partida_contable_cuenta ON detalles_partida_contable(cuenta_contable_id);

COMMENT ON TABLE detalles_partida_contable IS 'Línea de detalle de una partida contable (débito o crédito)';
COMMENT ON COLUMN detalles_partida_contable.debito IS 'Monto del débito. Solo uno de débito/crédito debe ser > 0 por línea.';
COMMENT ON COLUMN detalles_partida_contable.credito IS 'Monto del crédito. Solo uno de débito/crédito debe ser > 0 por línea.';

-- ============================================================================
-- PARTE 2: MODIFICAR TABLAS EXISTENTES
-- ============================================================================

-- 2.1 Agregar columna cuenta_contable_id a categorias_ingreso
ALTER TABLE categorias_ingreso
ADD COLUMN IF NOT EXISTS cuenta_contable_id BIGINT
REFERENCES cuentas_contables(id) ON DELETE SET NULL;

CREATE INDEX IF NOT EXISTS ix_categorias_ingreso_cuenta_contable
ON categorias_ingreso(cuenta_contable_id);

-- 2.2 Agregar columna cuenta_contable_id a categorias_egreso
ALTER TABLE categorias_egreso
ADD COLUMN IF NOT EXISTS cuenta_contable_id BIGINT
REFERENCES cuentas_contables(id) ON DELETE SET NULL;

CREATE INDEX IF NOT EXISTS ix_categorias_egreso_cuenta_contable
ON categorias_egreso(cuenta_contable_id);

-- ============================================================================
-- PARTE 3: CATÁLOGO DE CUENTAS — DATOS SEMILLA
-- ============================================================================

-- 3.1 ACTIVO (1)
INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion) VALUES
('1', 'ACTIVO', NULL, 1, TRUE, NOW());

WITH c AS (SELECT id FROM cuentas_contables WHERE codigo = '1')
INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
SELECT '1.1', 'Activo Corriente', c.id, 1, TRUE, NOW() FROM c;

WITH padre AS (SELECT id FROM cuentas_contables WHERE codigo = '1.1')
INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
SELECT '1.1.01', 'Caja General', padre.id, 1, TRUE, NOW() FROM padre
UNION ALL SELECT '1.1.02', 'Bancos', padre.id, 1, TRUE, NOW() FROM padre
UNION ALL SELECT '1.1.03', 'Cuentas por Cobrar', padre.id, 1, TRUE, NOW() FROM padre
UNION ALL SELECT '1.1.04', 'Inventarios', padre.id, 1, TRUE, NOW() FROM padre;

WITH c AS (SELECT id FROM cuentas_contables WHERE codigo = '1')
INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
SELECT '1.2', 'Activo No Corriente', c.id, 1, TRUE, NOW() FROM c;

WITH padre AS (SELECT id FROM cuentas_contables WHERE codigo = '1.2')
INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
SELECT '1.2.01', 'Propiedades y Equipos', padre.id, 1, TRUE, NOW() FROM padre
UNION ALL SELECT '1.2.02', 'Depreciación Acumulada', padre.id, 1, TRUE, NOW() FROM padre;

-- 3.2 PASIVO (2)
WITH c AS (SELECT id FROM cuentas_contables WHERE codigo = '1')
INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
SELECT '2', 'PASIVO', NULL, 2, TRUE, NOW()
WHERE NOT EXISTS (SELECT 1 FROM cuentas_contables WHERE codigo = '2');

WITH c AS (SELECT id FROM cuentas_contables WHERE codigo = '2')
INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
SELECT '2.1', 'Pasivo Corriente', c.id, 2, TRUE, NOW() FROM c;

WITH padre AS (SELECT id FROM cuentas_contables WHERE codigo = '2.1')
INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
SELECT '2.1.01', 'Cuentas por Pagar', padre.id, 2, TRUE, NOW() FROM padre
UNION ALL SELECT '2.1.02', 'Obligaciones Tributarias', padre.id, 2, TRUE, NOW() FROM padre
UNION ALL SELECT '2.1.03', 'Acreedores Varios', padre.id, 2, TRUE, NOW() FROM padre;

-- 3.3 CAPITAL (3)
INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
SELECT '3', 'CAPITAL', NULL, 3, TRUE, NOW()
WHERE NOT EXISTS (SELECT 1 FROM cuentas_contables WHERE codigo = '3');

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
SELECT '3.1.01', 'Capital Institucional', c.id, 3, TRUE, NOW() FROM cuentas_contables c WHERE c.codigo = '3'
UNION ALL SELECT '3.1.02', 'Resultados Acumulados', c.id, 3, TRUE, NOW() FROM cuentas_contables c WHERE c.codigo = '3'
UNION ALL SELECT '3.1.03', 'Resultado del Ejercicio', c.id, 3, TRUE, NOW() FROM cuentas_contables c WHERE c.codigo = '3'
ON CONFLICT (codigo) DO NOTHING;

-- 3.4 INGRESOS (4)
INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
SELECT '4', 'INGRESOS', NULL, 4, TRUE, NOW()
WHERE NOT EXISTS (SELECT 1 FROM cuentas_contables WHERE codigo = '4');

WITH padre AS (SELECT id FROM cuentas_contables WHERE codigo = '4')
INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
SELECT '4.1.01', 'Ingresos por Diezmos', padre.id, 4, TRUE, NOW() FROM padre
UNION ALL SELECT '4.1.02', 'Ingresos por Ofrendas', padre.id, 4, TRUE, NOW() FROM padre
UNION ALL SELECT '4.1.03', 'Ingresos por Colaboraciones', padre.id, 4, TRUE, NOW() FROM padre
UNION ALL SELECT '4.1.04', 'Ingresos por Eventos', padre.id, 4, TRUE, NOW() FROM padre
UNION ALL SELECT '4.1.99', 'Otros Ingresos', padre.id, 4, TRUE, NOW() FROM padre;

-- 3.5 GASTOS (5)
INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
SELECT '5', 'GASTOS', NULL, 5, TRUE, NOW()
WHERE NOT EXISTS (SELECT 1 FROM cuentas_contables WHERE codigo = '5');

WITH padre AS (SELECT id FROM cuentas_contables WHERE codigo = '5')
INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
SELECT '5.1.01', 'Gastos Operativos', padre.id, 5, TRUE, NOW() FROM padre
UNION ALL SELECT '5.1.02', 'Gastos de Personal', padre.id, 5, TRUE, NOW() FROM padre
UNION ALL SELECT '5.1.03', 'Servicios Públicos', padre.id, 5, TRUE, NOW() FROM padre
UNION ALL SELECT '5.1.04', 'Mantenimiento', padre.id, 5, TRUE, NOW() FROM padre
UNION ALL SELECT '5.1.05', 'Gastos de Transporte', padre.id, 5, TRUE, NOW() FROM padre
UNION ALL SELECT '5.1.06', 'Materiales y Suministros', padre.id, 5, TRUE, NOW() FROM padre
UNION ALL SELECT '5.1.99', 'Otros Gastos', padre.id, 5, TRUE, NOW() FROM padre;

-- ============================================================================
-- PARTE 4: CONFIGURACIÓN — CUENTA DE CAJA POR DEFECTO
-- ============================================================================

-- Registrar la cuenta Caja General (1.1.01) como la cuenta de contrapartida default
INSERT INTO configuracion_sistema (clave, valor, tipo_dato, categoria, grupo, descripcion, es_editable, es_publico, orden)
SELECT 'CUENTA_CAJA_DEFAULT_ID',
       cc.id::TEXT,
       'NUMERO',
       'CONTABILIDAD',
       'CONTABILIDAD_PARTIDA_DOBLE',
       'ID de la cuenta contable de caja/bancos usada como contrapartida por defecto en asientos automáticos',
       TRUE,
       FALSE,
       100
FROM cuentas_contables cc
WHERE cc.codigo = '1.1.01'
  AND NOT EXISTS (SELECT 1 FROM configuracion_sistema WHERE clave = 'CUENTA_CAJA_DEFAULT_ID');

-- ============================================================================
-- PARTE 5: MÓDULOS Y PERMISOS PARA EL MENÚ
-- ============================================================================

-- 5.1 Insertar módulo Contabilidad Avanzada (raíz, si no existe)
INSERT INTO modulos (id, nombre, icono, orden, activo, creado_en, parent_id)
SELECT COALESCE(MAX(id), 0) + 1, 'Contabilidad Avanzada', 'bi-calculator',
       COALESCE(MAX(orden), 0) + 10, TRUE, NOW(), NULL
FROM modulos
WHERE NOT EXISTS (SELECT 1 FROM modulos WHERE nombre = 'Contabilidad Avanzada');

-- 5.2 Insertar los 4 permisos granulares
DO $$
DECLARE
    v_modulo_id INTEGER;
    v_rol_admin_id INTEGER;
BEGIN
    SELECT id INTO v_modulo_id FROM modulos WHERE nombre = 'Contabilidad Avanzada';
    SELECT id INTO v_rol_admin_id FROM roles_sistema WHERE nombre = 'Administrador' LIMIT 1;

    IF v_modulo_id IS NOT NULL THEN
        -- Permiso 1: Catálogo de Cuentas
        INSERT INTO permisos (modulo_id, codigo, nombre, descripcion, url, icono, orden, es_menu, creado_en)
        VALUES (v_modulo_id, 'contabilidad.catalogo_cuentas', 'Catálogo de Cuentas',
                'Gestionar el catálogo de cuentas contables (alta, baja, modificaciones)',
                '/CatalogoCuentas', 'bi-journal-bookmark-fill', 1, TRUE, NOW())
        ON CONFLICT (codigo) DO NOTHING;

        -- Permiso 2: Partidas Contables
        INSERT INTO permisos (modulo_id, codigo, nombre, descripcion, url, icono, orden, es_menu, creado_en)
        VALUES (v_modulo_id, 'contabilidad.crear_partidas', 'Partidas Contables',
                'Registrar asientos contables de diario (débitos y créditos)',
                '/PartidasContables', 'bi-journal-text', 2, TRUE, NOW())
        ON CONFLICT (codigo) DO NOTHING;

        -- Permiso 3: Reportes Contables
        INSERT INTO permisos (modulo_id, codigo, nombre, descripcion, url, icono, orden, es_menu, creado_en)
        VALUES (v_modulo_id, 'contabilidad.ver_reportes', 'Reportes Contables',
                'Consultar Balance General y Estado de Resultados',
                '/ReportesContables', 'bi-file-earmark-bar-graph', 3, TRUE, NOW())
        ON CONFLICT (codigo) DO NOTHING;

        -- Permiso 4: Períodos Contables
        INSERT INTO permisos (modulo_id, codigo, nombre, descripcion, url, icono, orden, es_menu, creado_en)
        VALUES (v_modulo_id, 'contabilidad.cerrar_periodos', 'Períodos Contables',
                'Cerrar y reabrir períodos contables (bloquear/desbloquear ediciones)',
                '/PeriodoContable', 'bi-lock-fill', 4, TRUE, NOW())
        ON CONFLICT (codigo) DO NOTHING;

        -- 5.3 Asignar todos los permisos al rol Administrador
        IF v_rol_admin_id IS NOT NULL THEN
            INSERT INTO roles_permisos (rol_id, permiso_id, asignado_en)
            SELECT v_rol_admin_id, p.id, NOW()
            FROM permisos p
            WHERE p.codigo LIKE 'contabilidad.%'
              AND p.modulo_id = v_modulo_id
              AND NOT EXISTS (
                  SELECT 1 FROM roles_permisos rp
                  WHERE rp.rol_id = v_rol_admin_id AND rp.permiso_id = p.id
              );
        END IF;
    END IF;
END $$;

-- ============================================================================
-- PARTE 6: MIGRACIÓN DE DATOS HISTÓRICOS (opcional)
-- ============================================================================
-- Convierte los registros existentes de MovimientoGeneral y ContabilidadRegistro
-- en asientos de partida doble en las nuevas tablas.
-- ============================================================================
-- NOTA: Esta migración requiere que el catálogo de cuentas ya esté poblado
-- y que las categorías de ingreso/egreso tengan asignada una cuenta contable.
-- ============================================================================

-- 6.1 Migrar MovimientosGenerales de INGRESO a partidas contables
-- Cada movimiento de ingreso genera: Débito → Caja General, Crédito → cuenta de la categoría
INSERT INTO partidas_contables (fecha, referencia, descripcion, periodo_contable_id, cerrada, movimiento_general_id, fecha_creacion)
SELECT
    m.fecha,
    COALESCE(m.numero_comprobante, 'MIGRACION-MOV-' || m.id),
    COALESCE(m.descripcion, 'Migración automática de movimiento #' || m.id),
    NULL,  -- período se asigna después si existe
    TRUE,  -- migración marcada como cerrada
    m.id,
    NOW()
FROM movimientos_generales m
WHERE m.tipo = 1  -- Ingreso
  AND NOT EXISTS (SELECT 1 FROM partidas_contables pc WHERE pc.movimiento_general_id = m.id);

-- Insertar líneas de débito (Caja General) para cada partida migrada de ingreso
INSERT INTO detalles_partida_contable (partida_contable_id, cuenta_contable_id, debito, credito, descripcion)
SELECT
    pc.id,
    cc.id,  -- Caja General
    m.monto,
    0,
    'Débito automático - ' || COALESCE(m.descripcion, 'Migración movimiento #' || m.id)
FROM movimientos_generales m
JOIN partidas_contables pc ON pc.movimiento_general_id = m.id
CROSS JOIN cuentas_contables cc
WHERE cc.codigo = '1.1.01'
  AND m.tipo = 1
  AND NOT EXISTS (SELECT 1 FROM detalles_partida_contable d WHERE d.partida_contable_id = pc.id AND d.debito > 0);

-- Insertar líneas de crédito (cuenta de la categoría) para cada partida migrada de ingreso
INSERT INTO detalles_partida_contable (partida_contable_id, cuenta_contable_id, debito, credito, descripcion)
SELECT
    pc.id,
    COALESCE(ci.cuenta_contable_id, (SELECT id FROM cuentas_contables WHERE codigo = '4.1.99')),
    0,
    m.monto,
    'Crédito automático - ' || COALESCE(m.descripcion, 'Migración movimiento #' || m.id)
FROM movimientos_generales m
JOIN partidas_contables pc ON pc.movimiento_general_id = m.id
LEFT JOIN categorias_ingreso ci ON ci.id = m.categoria_ingreso_id
WHERE m.tipo = 1
  AND NOT EXISTS (SELECT 1 FROM detalles_partida_contable d WHERE d.partida_contable_id = pc.id AND d.credito > 0);

-- 6.2 Migrar MovimientosGenerales de EGRESO a partidas contables
-- Cada movimiento de egreso genera: Débito → cuenta de la categoría, Crédito → Caja General
INSERT INTO partidas_contables (fecha, referencia, descripcion, periodo_contable_id, cerrada, movimiento_general_id, fecha_creacion)
SELECT
    m.fecha,
    COALESCE(m.numero_comprobante, 'MIGRACION-MOV-' || m.id),
    COALESCE(m.descripcion, 'Migración automática de movimiento #' || m.id),
    NULL,
    TRUE,
    m.id,
    NOW()
FROM movimientos_generales m
WHERE m.tipo = 2  -- Egreso
  AND NOT EXISTS (SELECT 1 FROM partidas_contables pc WHERE pc.movimiento_general_id = m.id);

-- Insertar líneas de débito (cuenta de la categoría) para egreso
INSERT INTO detalles_partida_contable (partida_contable_id, cuenta_contable_id, debito, credito, descripcion)
SELECT
    pc.id,
    COALESCE(ce.cuenta_contable_id, (SELECT id FROM cuentas_contables WHERE codigo = '5.1.99')),
    m.monto,
    0,
    'Débito automático - ' || COALESCE(m.descripcion, 'Migración movimiento #' || m.id)
FROM movimientos_generales m
JOIN partidas_contables pc ON pc.movimiento_general_id = m.id
LEFT JOIN categorias_egreso ce ON ce.id = m.categoria_egreso_id
WHERE m.tipo = 2
  AND NOT EXISTS (SELECT 1 FROM detalles_partida_contable d WHERE d.partida_contable_id = pc.id AND d.debito > 0);

-- Insertar líneas de crédito (Caja General) para egreso
INSERT INTO detalles_partida_contable (partida_contable_id, cuenta_contable_id, debito, credito, descripcion)
SELECT
    pc.id,
    cc.id,
    0,
    m.monto,
    'Crédito automático - ' || COALESCE(m.descripcion, 'Migración movimiento #' || m.id)
FROM movimientos_generales m
JOIN partidas_contables pc ON pc.movimiento_general_id = m.id
CROSS JOIN cuentas_contables cc
WHERE cc.codigo = '1.1.01'
  AND m.tipo = 2
  AND NOT EXISTS (SELECT 1 FROM detalles_partida_contable d WHERE d.partida_contable_id = pc.id AND d.credito > 0);

-- 6.3 Migrar ContabilidadRegistros a partidas contables
-- Usa cuentas genéricas "4.1.99 - Otros Ingresos" y "5.1.99 - Otros Gastos"
INSERT INTO partidas_contables (fecha, referencia, descripcion, periodo_contable_id, cerrada, contabilidad_registro_id, fecha_creacion)
SELECT
    r.fecha,
    'LEGACY-CR-' || r.id,
    COALESCE(r.descripcion, 'Migración de registro contable legacy #' || r.id),
    NULL,
    TRUE,
    r.id,
    NOW()
FROM contabilidad_registros r
WHERE NOT EXISTS (SELECT 1 FROM partidas_contables pc WHERE pc.contabilidad_registro_id = r.id);

-- INSERTAR líneas para ContabilidadRegistros de INGRESO (Débito Caja, Crédito 4.1.99)
INSERT INTO detalles_partida_contable (partida_contable_id, cuenta_contable_id, debito, credito, descripcion)
SELECT
    pc.id,
    cc.id,
    r.monto, 0,
    'Débito automático - ' || COALESCE(r.descripcion, 'Migración CR #' || r.id)
FROM contabilidad_registros r
JOIN partidas_contables pc ON pc.contabilidad_registro_id = r.id
CROSS JOIN cuentas_contables cc
WHERE cc.codigo = '1.1.01'
  AND r.tipo = 1  -- Ingreso
  AND NOT EXISTS (SELECT 1 FROM detalles_partida_contable d WHERE d.partida_contable_id = pc.id AND d.debito > 0);

INSERT INTO detalles_partida_contable (partida_contable_id, cuenta_contable_id, debito, credito, descripcion)
SELECT
    pc.id,
    (SELECT id FROM cuentas_contables WHERE codigo = '4.1.99'),
    0, r.monto,
    'Crédito automático - ' || COALESCE(r.descripcion, 'Migración CR #' || r.id)
FROM contabilidad_registros r
JOIN partidas_contables pc ON pc.contabilidad_registro_id = r.id
WHERE r.tipo = 1
  AND NOT EXISTS (SELECT 1 FROM detalles_partida_contable d WHERE d.partida_contable_id = pc.id AND d.credito > 0);

-- INSERTAR líneas para ContabilidadRegistros de EGRESO (Débito 5.1.99, Crédito Caja)
INSERT INTO detalles_partida_contable (partida_contable_id, cuenta_contable_id, debito, credito, descripcion)
SELECT
    pc.id,
    (SELECT id FROM cuentas_contables WHERE codigo = '5.1.99'),
    r.monto, 0,
    'Débito automático - ' || COALESCE(r.descripcion, 'Migración CR #' || r.id)
FROM contabilidad_registros r
JOIN partidas_contables pc ON pc.contabilidad_registro_id = r.id
WHERE r.tipo = 2  -- Egreso
  AND NOT EXISTS (SELECT 1 FROM detalles_partida_contable d WHERE d.partida_contable_id = pc.id AND d.debito > 0);

INSERT INTO detalles_partida_contable (partida_contable_id, cuenta_contable_id, debito, credito, descripcion)
SELECT
    pc.id,
    cc.id,
    0, r.monto,
    'Crédito automático - ' || COALESCE(r.descripcion, 'Migración CR #' || r.id)
FROM contabilidad_registros r
JOIN partidas_contables pc ON pc.contabilidad_registro_id = r.id
CROSS JOIN cuentas_contables cc
WHERE cc.codigo = '1.1.01'
  AND r.tipo = 2
  AND NOT EXISTS (SELECT 1 FROM detalles_partida_contable d WHERE d.partida_contable_id = pc.id AND d.credito > 0);

-- ============================================================================
-- PARTE 7: ASIGNAR PERÍODOS CONTABLES A PARTIDAS MIGRADAS
-- ============================================================================
-- Crea períodos para los meses/años que tengan partidas migradas sin período
INSERT INTO periodos_contables (mes, anio, fecha_inicio, fecha_fin, cerrado, saldo_inicial, fecha_creacion)
SELECT DISTINCT
    EXTRACT(MONTH FROM pc.fecha)::INT,
    EXTRACT(YEAR FROM pc.fecha)::INT,
    DATE_TRUNC('month', pc.fecha)::DATE,
    (DATE_TRUNC('month', pc.fecha) + INTERVAL '1 month' - INTERVAL '1 day')::DATE,
    TRUE,  -- períodos históricos se marcan como cerrados
    0,
    NOW()
FROM partidas_contables pc
WHERE pc.periodo_contable_id IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM periodos_contables pp
      WHERE pp.mes = EXTRACT(MONTH FROM pc.fecha)::INT
        AND pp.anio = EXTRACT(YEAR FROM pc.fecha)::INT
  );

-- Asignar período a partidas que no lo tienen
UPDATE partidas_contables pc
SET periodo_contable_id = pp.id
FROM periodos_contables pp
WHERE pc.periodo_contable_id IS NULL
  AND pp.mes = EXTRACT(MONTH FROM pc.fecha)::INT
  AND pp.anio = EXTRACT(YEAR FROM pc.fecha)::INT;

-- ============================================================================
-- PARTE 8: VERIFICACIÓN FINAL
-- ============================================================================

DO $$
DECLARE
    v_total_cuentas INTEGER;
    v_total_partidas INTEGER;
    v_total_detalles INTEGER;
    v_partidas_no_balanceadas INTEGER;
    v_movimientos_migrados INTEGER;
    v_registros_migrados INTEGER;
BEGIN
    SELECT COUNT(*) INTO v_total_cuentas FROM cuentas_contables;
    SELECT COUNT(*) INTO v_total_partidas FROM partidas_contables;
    SELECT COUNT(*) INTO v_total_detalles FROM detalles_partida_contable;

    SELECT COUNT(*) INTO v_partidas_no_balanceadas
    FROM (
        SELECT partida_contable_id
        FROM detalles_partida_contable
        GROUP BY partida_contable_id
        HAVING ABS(SUM(debito) - SUM(credito)) > 0.01
    ) sub;

    SELECT COUNT(*) INTO v_movimientos_migrados
    FROM partidas_contables WHERE movimiento_general_id IS NOT NULL;

    SELECT COUNT(*) INTO v_registros_migrados
    FROM partidas_contables WHERE contabilidad_registro_id IS NOT NULL;

    RAISE NOTICE '==========================================';
    RAISE NOTICE 'RESUMEN DE MIGRACIÓN - PARTIDA DOBLE';
    RAISE NOTICE '==========================================';
    RAISE NOTICE 'Cuentas en el catálogo:        %', v_total_cuentas;
    RAISE NOTICE 'Partidas contables creadas:    %', v_total_partidas;
    RAISE NOTICE 'Líneas de detalle insertadas:  %', v_total_detalles;
    RAISE NOTICE 'MovimientosGeneral migrados:   %', v_movimientos_migrados;
    RAISE NOTICE 'ContabilidadRegistros migrados:%', v_registros_migrados;

    IF v_partidas_no_balanceadas > 0 THEN
        RAISE WARNING '⚠ PARTIDAS NO BALANCEADAS: %', v_partidas_no_balanceadas;
    ELSE
        RAISE NOTICE '✅ Todas las partidas están balanceadas (Débito = Crédito).';
    END IF;

    RAISE NOTICE '==========================================';
END $$;

COMMIT;
