-- SQL para insertar módulo de Asistencia y permisos básicos
-- PostgreSQL
-- Ejecutar después de crear la tabla asistencias_culto

-- 1. Insertar módulo de Asistencia (si no existe)
INSERT INTO modulos (id, nombre, icono, orden, activo, creado_en, parent_id)
VALUES (
    (SELECT COALESCE(MAX(id), 0) + 1 FROM modulos),
    'Asistencia',
    'bi-people',
    (SELECT COALESCE(MAX(orden), 0) + 10 FROM modulos WHERE parent_id IS NULL),
    true,
    NOW(),
    NULL
)
ON CONFLICT (nombre) DO NOTHING;

-- Obtener el ID del módulo insertado (o existente)
DO $$
DECLARE
    modulo_asistencia_id INTEGER;
    rol_admin_id INTEGER;
BEGIN
    -- Obtener ID del módulo de Asistencia
    SELECT id INTO modulo_asistencia_id FROM modulos WHERE nombre = 'Asistencia';
    
    -- Obtener ID del rol Administrador (asumiendo que existe)
    SELECT id INTO rol_admin_id FROM roles_sistema WHERE nombre = 'Administrador' LIMIT 1;
    
    -- 2. Insertar permisos básicos para el módulo de Asistencia
    -- Permiso: Ver asistencias
    INSERT INTO permisos (modulo_id, codigo, nombre, descripcion, url, icono, orden, es_menu, creado_en)
    VALUES (
        modulo_asistencia_id,
        'asistencia.ver',
        'Ver Asistencias',
        'Permite ver el listado de asistencias de cultos',
        '/AsistenciaCulto',
        'bi-eye',
        1,
        true,
        NOW()
    )
    ON CONFLICT (codigo) DO NOTHING;
    
    -- Permiso: Crear asistencias
    INSERT INTO permisos (modulo_id, codigo, nombre, descripcion, url, icono, orden, es_menu, creado_en)
    VALUES (
        modulo_asistencia_id,
        'asistencia.crear',
        'Crear Asistencia',
        'Permite registrar nueva asistencia de culto',
        '/AsistenciaCulto/Create',
        'bi-plus-circle',
        2,
        true,
        NOW()
    )
    ON CONFLICT (codigo) DO NOTHING;
    
    -- Permiso: Editar asistencias
    INSERT INTO permisos (modulo_id, codigo, nombre, descripcion, url, icono, orden, es_menu, creado_en)
    VALUES (
        modulo_asistencia_id,
        'asistencia.editar',
        'Editar Asistencia',
        'Permite editar registros de asistencia existentes',
        '/AsistenciaCulto/Edit',
        'bi-pencil',
        3,
        true,
        NOW()
    )
    ON CONFLICT (codigo) DO NOTHING;
    
    -- Permiso: Eliminar asistencias
    INSERT INTO permisos (modulo_id, codigo, nombre, descripcion, url, icono, orden, es_menu, creado_en)
    VALUES (
        modulo_asistencia_id,
        'asistencia.eliminar',
        'Eliminar Asistencia',
        'Permite eliminar registros de asistencia',
        '/AsistenciaCulto/Delete',
        'bi-trash',
        4,
        true,
        NOW()
    )
    ON CONFLICT (codigo) DO NOTHING;
    
    -- 3. Asignar permisos al rol Administrador (si existe)
    IF rol_admin_id IS NOT NULL THEN
        -- Asignar permiso: asistencia.ver
        INSERT INTO roles_permisos (rol_id, permiso_id)
        SELECT rol_admin_id, id
        FROM permisos 
        WHERE codigo = 'asistencia.ver'
        ON CONFLICT (rol_id, permiso_id) DO NOTHING;
        
        -- Asignar permiso: asistencia.crear
        INSERT INTO roles_permisos (rol_id, permiso_id)
        SELECT rol_admin_id, id
        FROM permisos 
        WHERE codigo = 'asistencia.crear'
        ON CONFLICT (rol_id, permiso_id) DO NOTHING;
        
        -- Asignar permiso: asistencia.editar
        INSERT INTO roles_permisos (rol_id, permiso_id)
        SELECT rol_admin_id, id
        FROM permisos 
        WHERE codigo = 'asistencia.editar'
        ON CONFLICT (rol_id, permiso_id) DO NOTHING;
        
        -- Asignar permiso: asistencia.eliminar
        INSERT INTO roles_permisos (rol_id, permiso_id)
        SELECT rol_admin_id, id
        FROM permisos 
        WHERE codigo = 'asistencia.eliminar'
        ON CONFLICT (rol_id, permiso_id) DO NOTHING;
        
        RAISE NOTICE 'Permisos de Asistencia asignados al rol Administrador (ID: %)', rol_admin_id;
    ELSE
        RAISE NOTICE 'Rol Administrador no encontrado. Los permisos deben asignarse manualmente.';
    END IF;
    
    RAISE NOTICE 'Módulo de Asistencia y permisos configurados correctamente.';
END $$;

-- Verificación final
SELECT 
    m.nombre as modulo,
    p.codigo as permiso,
    p.nombre as nombre_permiso,
    p.url
FROM modulos m
JOIN permisos p ON m.id = p.modulo_id
WHERE m.nombre = 'Asistencia'
ORDER BY p.orden;