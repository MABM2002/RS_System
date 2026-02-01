-- ============================================
-- Script: Agregar Gestión de Tipos de Colaboración
-- Descripción: Agrega el módulo y permiso para gestionar tipos de colaboración
-- Fecha: 2026-02-01
-- NOTA: Este script NO debe ejecutarse automáticamente, el usuario lo ejecutará manualmente
-- ============================================

-- 1. Insertar permiso para Tipo Colaboración (si no existe)
DO $$
BEGIN
    -- Verificar si ya existe el módulo de Finanzas
    IF NOT EXISTS (SELECT 1 FROM public.modulos WHERE codigo = 'FINANZAS') THEN
        INSERT INTO public.modulos (nombre, descripcion, codigo, icono, activo, orden, creado_en, actualizado_en)
        VALUES ('Finanzas', 'Módulo de gestión financiera', 'FINANZAS', 'bi-cash-stack', true, 5, NOW(), NOW());
    END IF;

    -- Insertar permiso para Colaboraciones (si no existe)
    IF NOT EXISTS (SELECT 1 FROM public.permisos WHERE codigo = 'Colaboracion') THEN
        INSERT INTO public.permisos (nombre, descripcion, codigo, modulo_id, activo, orden, creado_en, actualizado_en)
        VALUES (
            'Colaboraciones',
            'Gestión de colaboraciones económicas mensuales',
            'Colaboracion',
            (SELECT id FROM public.modulos WHERE codigo = 'FINANZAS' LIMIT 1),
            true,
            1,
            NOW(),
            NOW()
        );
    END IF;

    -- Insertar permiso para Tipos de Colaboración (si no existe)
    IF NOT EXISTS (SELECT 1 FROM public.permisos WHERE codigo = 'TipoColaboracion') THEN
        INSERT INTO public.permisos (nombre, descripcion, codigo, modulo_id, activo, orden, creado_en, actualizado_en)
        VALUES (
            'Tipos de Colaboración',
            'Gestión de tipos de colaboración (Transporte, Limpieza, etc.)',
            'TipoColaboracion',
            (SELECT id FROM public.modulos WHERE codigo = 'FINANZAS' LIMIT 1),
            true,
            2,
            NOW(),
            NOW()
        );
    END IF;

    RAISE NOTICE 'Permisos para Colaboraciones creados exitosamente';
END $$;

-- 2. (Opcional) Asignar permisos al rol de Administrador
-- Descomentar si se desea asignar automáticamente
/*
DO $$
DECLARE
    v_rol_admin_id BIGINT;
    v_permiso_colaboracion_id BIGINT;
    v_permiso_tipo_id BIGINT;
BEGIN
    -- Obtener el ID del rol de administrador (ajustar el nombre según tu sistema)
    SELECT id INTO v_rol_admin_id FROM public.roles_sistema WHERE nombre = 'Administrador' LIMIT 1;
    
    -- Obtener IDs de los permisos
    SELECT id INTO v_permiso_colaboracion_id FROM public.permisos WHERE codigo = 'Colaboracion' LIMIT 1;
    SELECT id INTO v_permiso_tipo_id FROM public.permisos WHERE codigo = 'TipoColaboracion' LIMIT 1;
    
    IF v_rol_admin_id IS NOT NULL THEN
        -- Asignar permiso de Colaboraciones
        IF NOT EXISTS (SELECT 1 FROM public.roles_permisos WHERE rol_id = v_rol_admin_id AND permiso_id = v_permiso_colaboracion_id) THEN
            INSERT INTO public.roles_permisos (rol_id, permiso_id, creado_en)
            VALUES (v_rol_admin_id, v_permiso_colaboracion_id, NOW());
        END IF;
        
        -- Asignar permiso de Tipos de Colaboración
        IF NOT EXISTS (SELECT 1 FROM public.roles_permisos WHERE rol_id = v_rol_admin_id AND permiso_id = v_permiso_tipo_id) THEN
            INSERT INTO public.roles_permisos (rol_id, permiso_id, creado_en)
            VALUES (v_rol_admin_id, v_permiso_tipo_id, NOW());
        END IF;
        
        RAISE NOTICE 'Permisos asignados al rol Administrador';
    END IF;
END $$;
*/

-- 3. Verificación: Listar permisos creados
SELECT 
    m.nombre AS modulo,
    p.nombre AS permiso,
    p.codigo,
    p.activo
FROM public.permisos p
INNER JOIN public.modulos m ON p.modulo_id = m.id
WHERE p.codigo IN ('Colaboracion', 'TipoColaboracion')
ORDER BY m.nombre, p.orden;

-- ============================================
-- FIN DEL SCRIPT
-- ============================================
