-- SQL para insertar módulo de Contabilidad por Partida Doble y sus permisos
-- PostgreSQL
-- Ejecutar DESPUÉS de ejecutar SeedCatalogoCuentas.sql

-- 1. Insertar módulo Contabilidad (si no existe como módulo raíz)
INSERT INTO modulos (id, nombre, icono, orden, activo, creado_en, parent_id)
SELECT 
    (SELECT COALESCE(MAX(id), 0) + 1 FROM modulos),
    'Contabilidad Avanzada',
    'bi-calculator',
    (SELECT COALESCE(MAX(orden), 0) + 10 FROM modulos WHERE parent_id IS NULL),
    true,
    NOW(),
    NULL
WHERE NOT EXISTS (SELECT 1 FROM modulos WHERE nombre = 'Contabilidad Avanzada')
ON CONFLICT (nombre) DO NOTHING;

-- Obtener el ID del módulo y rol admin
DO $$
DECLARE
    modulo_contable_id INTEGER;
    rol_admin_id INTEGER;
BEGIN
    SELECT id INTO modulo_contable_id FROM modulos WHERE nombre = 'Contabilidad Avanzada';
    SELECT id INTO rol_admin_id FROM roles_sistema WHERE nombre = 'Administrador' LIMIT 1;

    -- 2. Insertar permisos (4 permisos granulares según decisión del usuario)

    -- Permiso: Catálogo de Cuentas
    INSERT INTO permisos (modulo_id, codigo, nombre, descripcion, url, icono, orden, es_menu, creado_en)
    VALUES (
        modulo_contable_id,
        'contabilidad.catalogo_cuentas',
        'Catálogo de Cuentas',
        'Gestionar el catálogo de cuentas contables (alta, baja, modificaciones)',
        '/CatalogoCuentas',
        'bi-journal-bookmark-fill',
        1,
        true,
        NOW()
    )
    ON CONFLICT (codigo) DO NOTHING;

    -- Permiso: Crear Partidas Contables
    INSERT INTO permisos (modulo_id, codigo, nombre, descripcion, url, icono, orden, es_menu, creado_en)
    VALUES (
        modulo_contable_id,
        'contabilidad.crear_partidas',
        'Partidas Contables',
        'Registrar asientos contables de diario (débitos y créditos)',
        '/PartidasContables',
        'bi-journal-text',
        2,
        true,
        NOW()
    )
    ON CONFLICT (codigo) DO NOTHING;

    -- Permiso: Ver Reportes Contables
    INSERT INTO permisos (modulo_id, codigo, nombre, descripcion, url, icono, orden, es_menu, creado_en)
    VALUES (
        modulo_contable_id,
        'contabilidad.ver_reportes',
        'Reportes Contables',
        'Consultar Balance General y Estado de Resultados',
        '/ReportesContables',
        'bi-file-earmark-bar-graph',
        3,
        true,
        NOW()
    )
    ON CONFLICT (codigo) DO NOTHING;

    -- Permiso: Cerrar Períodos Contables
    INSERT INTO permisos (modulo_id, codigo, nombre, descripcion, url, icono, orden, es_menu, creado_en)
    VALUES (
        modulo_contable_id,
        'contabilidad.cerrar_periodos',
        'Períodos Contables',
        'Cerrar y reabrir períodos contables (bloquear/desbloquear ediciones)',
        '/PeriodoContable',
        'bi-lock-fill',
        4,
        true,
        NOW()
    )
    ON CONFLICT (codigo) DO NOTHING;

    -- 3. Asignar todos los permisos al rol Administrador (si existe)
    IF rol_admin_id IS NOT NULL THEN
        INSERT INTO roles_permisos (rol_id, permiso_id)
        SELECT rol_admin_id, id
        FROM permisos 
        WHERE codigo LIKE 'contabilidad.%'
        ON CONFLICT (rol_id, permiso_id) DO NOTHING;
    END IF;
END $$;
