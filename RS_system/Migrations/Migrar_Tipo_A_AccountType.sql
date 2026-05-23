-- ============================================================================
-- SCRIPT DE MIGRACIÓN: Esquema antiguo (tipo enum) → Nuevo (AccountType FK)
-- PostgreSQL — Ejecutar directamente en pgAdmin, DBeaver, etc.
-- ============================================================================
-- Este script convierte la BD existente SIN usar EF Core migrations.
-- Preserva todos los datos existentes.
-- ============================================================================

BEGIN;

-- ============================================================================
-- PASO 1: Crear tabla tipos_cuenta_contable (si no existe)
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

-- Seed de los 5 tipos estándar (IDs 1-5 coinciden con antiguo enum TipoCuenta)
INSERT INTO tipos_cuenta_contable (id, nombre, naturaleza, categoria_reporte, orden, activo) VALUES
    (1, 'Activo',  1, 1, 1, TRUE),
    (2, 'Pasivo',  2, 1, 2, TRUE),
    (3, 'Capital', 2, 1, 3, TRUE),
    (4, 'Ingreso', 2, 2, 4, TRUE),
    (5, 'Gasto',   1, 2, 5, TRUE)
ON CONFLICT (id) DO NOTHING;

-- ============================================================================
-- PASO 2: Agregar columna account_type_id a cuentas_contables (si no existe)
-- ============================================================================
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'cuentas_contables' AND column_name = 'account_type_id'
    ) THEN
        ALTER TABLE cuentas_contables ADD COLUMN account_type_id INTEGER;
    END IF;
END $$;

-- ============================================================================
-- PASO 3: Migrar datos existentes de 'tipo' a 'account_type_id'
-- Los valores del antiguo enum coinciden exactamente con los IDs de tipos_cuenta_contable
-- TipoCuenta.Activo=1, Pasivo=2, Capital=3, Ingreso=4, Gasto=5
-- ============================================================================
UPDATE cuentas_contables
SET account_type_id = tipo
WHERE account_type_id IS NULL AND tipo IS NOT NULL AND tipo BETWEEN 1 AND 5;

-- Si alguna cuenta tiene tipo NULL o fuera de rango, asignar Activo (1) por defecto
UPDATE cuentas_contables
SET account_type_id = 1
WHERE account_type_id IS NULL;

-- ============================================================================
-- PASO 4: Establecer FK y NOT NULL (ahora que los datos están migrados)
-- ============================================================================
-- Primero eliminar la FK antigua si existía (por el tipo int)
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_type = 'FOREIGN KEY' AND table_name = 'cuentas_contables'
        AND constraint_name LIKE '%tipo%'
    ) THEN
        EXECUTE 'ALTER TABLE cuentas_contables DROP CONSTRAINT ' || (
            SELECT constraint_name FROM information_schema.table_constraints
            WHERE constraint_type = 'FOREIGN KEY' AND table_name = 'cuentas_contables'
            AND constraint_name LIKE '%tipo%' LIMIT 1
        );
    END IF;
END $$;

-- Agregar FK a tipos_cuenta_contable (si no existe)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_type = 'FOREIGN KEY' AND table_name = 'cuentas_contables'
        AND constraint_name LIKE '%account_type%'
    ) THEN
        ALTER TABLE cuentas_contables
        ADD CONSTRAINT fk_cuentas_contables_account_type
        FOREIGN KEY (account_type_id) REFERENCES tipos_cuenta_contable(id) ON DELETE RESTRICT;
    END IF;
END $$;

-- Hacer NOT NULL la columna
ALTER TABLE cuentas_contables ALTER COLUMN account_type_id SET NOT NULL;

-- ============================================================================
-- PASO 5: Opcional — Eliminar columna 'tipo' antigua
-- Descomentar la siguiente línea cuando quieras eliminar la columna vieja
-- ============================================================================
-- ALTER TABLE cuentas_contables DROP COLUMN IF EXISTS tipo;

-- ============================================================================
-- PASO 6: Índices
-- ============================================================================
CREATE INDEX IF NOT EXISTS ix_cuentas_contables_account_type ON cuentas_contables(account_type_id);

-- ============================================================================
-- VERIFICACIÓN
-- ============================================================================
DO $$
DECLARE
    v_total INTEGER;
    v_migrados INTEGER;
    v_nulos INTEGER;
BEGIN
    SELECT COUNT(*) INTO v_total FROM cuentas_contables;
    SELECT COUNT(*) INTO v_migrados FROM cuentas_contables WHERE account_type_id IS NOT NULL;
    SELECT COUNT(*) INTO v_nulos FROM cuentas_contables WHERE account_type_id IS NULL;

    RAISE NOTICE '=============================================';
    RAISE NOTICE 'MIGRACIÓN COMPLETADA';
    RAISE NOTICE '=============================================';
    RAISE NOTICE 'Total cuentas en catálogo:       %', v_total;
    RAISE NOTICE 'Cuentas con account_type_id:     %', v_migrados;
    RAISE NOTICE 'Cuentas SIN account_type_id:     %', v_nulos;
    RAISE NOTICE 'Tipos de cuenta disponibles:     5 (Activo, Pasivo, Capital, Ingreso, Gasto)';
    RAISE NOTICE '=============================================';
    IF v_nulos > 0 THEN
        RAISE WARNING '⚠ Hay % cuentas sin account_type_id. Ejecute: UPDATE cuentas_contables SET account_type_id = 1 WHERE account_type_id IS NULL;', v_nulos;
    ELSE
        RAISE NOTICE '✅ Todas las cuentas tienen account_type_id asignado.';
    END IF;
END $$;

COMMIT;
