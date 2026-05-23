-- ============================================================================
-- SCRIPT SQL: Módulo de Contabilidad por Partida Doble (v2 - AccountType Dinámico)
-- PostgreSQL
-- ============================================================================
-- Crea tablas, índices, datos semilla y configura permisos para el módulo completo.
-- ============================================================================

BEGIN;

-- ============================================================================
-- PARTE 1: TIPOS DE CUENTA (AccountTypes) — Entidad dinámica
-- ============================================================================

CREATE TABLE IF NOT EXISTS tipos_cuenta_contable (
    id                SERIAL          PRIMARY KEY,
    nombre            VARCHAR(100)    NOT NULL,
    naturaleza        INTEGER         NOT NULL CHECK (naturaleza IN (1, 2)),
    -- 1 = Deudora, 2 = Acreedora
    categoria_reporte INTEGER         NOT NULL CHECK (categoria_reporte IN (1, 2)),
    -- 1 = Balance, 2 = Resultado
    orden             INTEGER         NOT NULL DEFAULT 0,
    activo            BOOLEAN         NOT NULL DEFAULT TRUE
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_tipos_cuenta_contable_nombre ON tipos_cuenta_contable(nombre);

COMMENT ON TABLE tipos_cuenta_contable IS 'Tipos de cuenta contable — dinámicos, administrables vía CRUD';
COMMENT ON COLUMN tipos_cuenta_contable.naturaleza IS '1=Deudora (saldo natural débito), 2=Acreedora (saldo natural crédito)';
COMMENT ON COLUMN tipos_cuenta_contable.categoria_reporte IS '1=Balance General, 2=Estado de Resultados';

-- Seed inicial de tipos de cuenta (5 estándar)
INSERT INTO tipos_cuenta_contable (nombre, naturaleza, categoria_reporte, orden, activo) VALUES
    ('Activo',  1, 1, 1, TRUE),
    ('Pasivo',  2, 1, 2, TRUE),
    ('Capital', 2, 1, 3, TRUE),
    ('Ingreso', 2, 2, 4, TRUE),
    ('Gasto',   1, 2, 5, TRUE)
ON CONFLICT (nombre) DO NOTHING;

-- ============================================================================
-- PARTE 2: CATÁLOGO DE CUENTAS
-- ============================================================================

CREATE TABLE IF NOT EXISTS cuentas_contables (
    id              BIGSERIAL       PRIMARY KEY,
    codigo          VARCHAR(20)     NOT NULL,
    nombre          VARCHAR(150)    NOT NULL,
    padre_id        BIGINT          REFERENCES cuentas_contables(id) ON DELETE RESTRICT,
    account_type_id INTEGER         NOT NULL REFERENCES tipos_cuenta_contable(id) ON DELETE RESTRICT,
    activa          BOOLEAN         NOT NULL DEFAULT TRUE,
    fecha_creacion  TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_cuentas_contables_codigo ON cuentas_contables(codigo);
CREATE INDEX IF NOT EXISTS ix_cuentas_contables_padre ON cuentas_contables(padre_id);
CREATE INDEX IF NOT EXISTS ix_cuentas_contables_type ON cuentas_contables(account_type_id);

-- ============================================================================
-- PARTE 3: PERÍODOS, PARTIDAS Y DETALLES
-- ============================================================================

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
CREATE UNIQUE INDEX IF NOT EXISTS uq_periodos_contables ON periodos_contables(mes, anio);

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
CREATE INDEX IF NOT EXISTS ix_partidas_periodo ON partidas_contables(periodo_contable_id);
CREATE INDEX IF NOT EXISTS ix_partidas_fecha ON partidas_contables(fecha);

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
CREATE INDEX IF NOT EXISTS ix_detalles_partida ON detalles_partida_contable(partida_contable_id);
CREATE INDEX IF NOT EXISTS ix_detalles_cuenta ON detalles_partida_contable(cuenta_contable_id);

-- ============================================================================
-- PARTE 4: MODIFICAR TABLAS EXISTENTES
-- ============================================================================

ALTER TABLE categorias_ingreso ADD COLUMN IF NOT EXISTS cuenta_contable_id BIGINT
    REFERENCES cuentas_contables(id) ON DELETE SET NULL;

ALTER TABLE categorias_egreso ADD COLUMN IF NOT EXISTS cuenta_contable_id BIGINT
    REFERENCES cuentas_contables(id) ON DELETE SET NULL;

-- ============================================================================
-- PARTE 5: SEMILLA DEL CATÁLOGO DE CUENTAS (códigos sin puntos)
-- ============================================================================

-- Raíces
INSERT INTO cuentas_contables (codigo, nombre, account_type_id, activa) VALUES
('1', 'ACTIVO',              (SELECT id FROM tipos_cuenta_contable WHERE nombre = 'Activo'),  TRUE),
('2', 'PASIVO',              (SELECT id FROM tipos_cuenta_contable WHERE nombre = 'Pasivo'),  TRUE),
('3', 'CAPITAL',             (SELECT id FROM tipos_cuenta_contable WHERE nombre = 'Capital'), TRUE),
('4', 'INGRESOS',            (SELECT id FROM tipos_cuenta_contable WHERE nombre = 'Ingreso'), TRUE),
('5', 'GASTOS',              (SELECT id FROM tipos_cuenta_contable WHERE nombre = 'Gasto'),   TRUE)
ON CONFLICT (codigo) DO NOTHING;

-- Activo Corriente (hijas de 1)
INSERT INTO cuentas_contables (codigo, nombre, padre_id, account_type_id, activa)
SELECT '11', 'Activo Corriente', c.id, t.id, TRUE
FROM cuentas_contables c, tipos_cuenta_contable t WHERE c.codigo = '1' AND t.nombre = 'Activo'
ON CONFLICT (codigo) DO NOTHING;

INSERT INTO cuentas_contables (codigo, nombre, padre_id, account_type_id, activa)
SELECT v.codigo, v.nombre, p.id, t.id, TRUE
FROM (VALUES ('1101','Caja General'),('1102','Bancos'),('1103','Cuentas por Cobrar'),('1104','Inventarios')) AS v(codigo,nombre)
CROSS JOIN cuentas_contables p CROSS JOIN tipos_cuenta_contable t
WHERE p.codigo = '11' AND t.nombre = 'Activo'
ON CONFLICT (codigo) DO NOTHING;

-- Activo No Corriente
INSERT INTO cuentas_contables (codigo, nombre, padre_id, account_type_id, activa)
SELECT '12', 'Activo No Corriente', c.id, t.id, TRUE
FROM cuentas_contables c, tipos_cuenta_contable t WHERE c.codigo = '1' AND t.nombre = 'Activo'
ON CONFLICT (codigo) DO NOTHING;

INSERT INTO cuentas_contables (codigo, nombre, padre_id, account_type_id, activa)
SELECT v.codigo, v.nombre, p.id, t.id, TRUE
FROM (VALUES ('1201','Propiedades y Equipos'),('1202','Depreciación Acumulada')) AS v(codigo,nombre)
CROSS JOIN cuentas_contables p CROSS JOIN tipos_cuenta_contable t
WHERE p.codigo = '12' AND t.nombre = 'Activo'
ON CONFLICT (codigo) DO NOTHING;

-- Pasivo Corriente
INSERT INTO cuentas_contables (codigo, nombre, padre_id, account_type_id, activa)
SELECT '21', 'Pasivo Corriente', c.id, t.id, TRUE
FROM cuentas_contables c, tipos_cuenta_contable t WHERE c.codigo = '2' AND t.nombre = 'Pasivo'
ON CONFLICT (codigo) DO NOTHING;

INSERT INTO cuentas_contables (codigo, nombre, padre_id, account_type_id, activa)
SELECT v.codigo, v.nombre, p.id, t.id, TRUE
FROM (VALUES ('2101','Cuentas por Pagar'),('2102','Obligaciones Tributarias'),('2103','Acreedores Varios')) AS v(codigo,nombre)
CROSS JOIN cuentas_contables p CROSS JOIN tipos_cuenta_contable t
WHERE p.codigo = '21' AND t.nombre = 'Pasivo'
ON CONFLICT (codigo) DO NOTHING;

-- Capital
INSERT INTO cuentas_contables (codigo, nombre, padre_id, account_type_id, activa)
SELECT v.codigo, v.nombre, c.id, t.id, TRUE
FROM (VALUES ('301','Capital Institucional'),('302','Resultados Acumulados'),('303','Resultado del Ejercicio')) AS v(codigo,nombre)
CROSS JOIN cuentas_contables c CROSS JOIN tipos_cuenta_contable t
WHERE c.codigo = '3' AND t.nombre = 'Capital'
ON CONFLICT (codigo) DO NOTHING;

-- Ingresos
INSERT INTO cuentas_contables (codigo, nombre, padre_id, account_type_id, activa)
SELECT v.codigo, v.nombre, c.id, t.id, TRUE
FROM (VALUES ('401','Diezmos'),('402','Ofrendas'),('403','Colaboraciones'),('404','Eventos'),('499','Otros Ingresos')) AS v(codigo,nombre)
CROSS JOIN cuentas_contables c CROSS JOIN tipos_cuenta_contable t
WHERE c.codigo = '4' AND t.nombre = 'Ingreso'
ON CONFLICT (codigo) DO NOTHING;

-- Gastos
INSERT INTO cuentas_contables (codigo, nombre, padre_id, account_type_id, activa)
SELECT v.codigo, v.nombre, c.id, t.id, TRUE
FROM (VALUES ('501','Gastos Operativos'),('502','Gastos de Personal'),('503','Servicios Públicos'),
             ('504','Mantenimiento'),('505','Transporte'),('506','Materiales y Suministros'),('599','Otros Gastos')) AS v(codigo,nombre)
CROSS JOIN cuentas_contables c CROSS JOIN tipos_cuenta_contable t
WHERE c.codigo = '5' AND t.nombre = 'Gasto'
ON CONFLICT (codigo) DO NOTHING;

-- ============================================================================
-- PARTE 6: CONFIGURACIÓN — CUENTA CAJA POR DEFECTO
-- ============================================================================

INSERT INTO configuracion_sistema (clave, valor, tipo_dato, categoria, grupo, descripcion, es_editable, es_publico, orden)
SELECT 'CUENTA_CAJA_DEFAULT_ID', cc.id::TEXT, 'NUMERO', 'CONTABILIDAD', 'CONTABILIDAD_PARTIDA_DOBLE',
       'ID de la cuenta contable de caja/bancos usada como contrapartida por defecto en asientos automáticos',
       TRUE, FALSE, 100
FROM cuentas_contables cc WHERE cc.codigo = '1101'
  AND NOT EXISTS (SELECT 1 FROM configuracion_sistema WHERE clave = 'CUENTA_CAJA_DEFAULT_ID');

-- ============================================================================
-- PARTE 7: MÓDULO Y PERMISOS EN MENÚ
-- ============================================================================

INSERT INTO modulos (id, nombre, icono, orden, activo, creado_en, parent_id)
SELECT COALESCE(MAX(id),0)+1, 'Contabilidad Avanzada', 'bi-calculator', COALESCE(MAX(orden),0)+10, TRUE, NOW(), NULL
FROM modulos WHERE NOT EXISTS (SELECT 1 FROM modulos WHERE nombre = 'Contabilidad Avanzada');

DO $$
DECLARE vm_id INT; vr_admin INT;
BEGIN
    SELECT id INTO vm_id FROM modulos WHERE nombre = 'Contabilidad Avanzada';
    SELECT id INTO vr_admin FROM roles_sistema WHERE nombre = 'Administrador' LIMIT 1;
    IF vm_id IS NOT NULL THEN
        INSERT INTO permisos (modulo_id, codigo, nombre, descripcion, url, icono, orden, es_menu, creado_en) VALUES
            (vm_id,'contabilidad.tipos_cuenta','Tipos de Cuenta','Administrar tipos de cuenta dinámicos (CRUD)','/AccountTypes','bi-tags',0,true,NOW()),
            (vm_id,'contabilidad.catalogo_cuentas','Catálogo de Cuentas','Gestionar el catálogo de cuentas jerárquico','/CatalogoCuentas','bi-journal-bookmark-fill',1,true,NOW()),
            (vm_id,'contabilidad.crear_partidas','Partidas Contables','Registrar asientos contables (débito/crédito)','/PartidasContables','bi-journal-text',2,true,NOW()),
            (vm_id,'contabilidad.ver_reportes','Reportes Contables','Balance General y Estado de Resultados','/ReportesContables','bi-file-earmark-bar-graph',3,true,NOW()),
            (vm_id,'contabilidad.cerrar_periodos','Períodos Contables','Cerrar y reabrir períodos','/PeriodoContable','bi-lock-fill',4,true,NOW())
        ON CONFLICT (codigo) DO NOTHING;

        IF vr_admin IS NOT NULL THEN
            INSERT INTO roles_permisos (rol_id, permiso_id, asignado_en)
            SELECT vr_admin, p.id, NOW() FROM permisos p WHERE p.codigo LIKE 'contabilidad.%' AND p.modulo_id = vm_id
            ON CONFLICT (rol_id, permiso_id) DO NOTHING;
        END IF;
    END IF;
END $$;

-- ============================================================================
-- PARTE 8: VERIFICACIÓN
-- ============================================================================

DO $$
DECLARE v_tipos INT; v_cuentas INT;
BEGIN
    SELECT COUNT(*) INTO v_tipos FROM tipos_cuenta_contable;
    SELECT COUNT(*) INTO v_cuentas FROM cuentas_contables;
    RAISE NOTICE '==========================================';
    RAISE NOTICE 'Tipos de cuenta insertados: %', v_tipos;
    RAISE NOTICE 'Cuentas contables insertadas: %', v_cuentas;
    RAISE NOTICE '==========================================';
END $$;

COMMIT;
