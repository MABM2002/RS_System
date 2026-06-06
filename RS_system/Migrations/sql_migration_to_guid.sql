-- =====================================================
-- SQL Migration Script: Auto-increment IDs to UUIDs
-- =====================================================
-- WARNING: This is a BREAKING CHANGE. Make a backup first!
-- This script converts all primary keys from BIGSERIAL to UUID
-- and updates all foreign key references accordingly.
-- 
-- INSTRUCTIONS:
-- 1. BACKUP your database first: pg_dump -U postgres rs_system > backup.sql
-- 2. Test on a copy of the database first
-- 3. Run during maintenance window (minimal user activity)
-- 4. Verify all data after migration
-- =====================================================

BEGIN;

-- =====================================================
-- STEP 1: Enable UUID extension
-- =====================================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =====================================================
-- STEP 2: Drop all foreign key constraints
-- =====================================================
-- (We'll recreate them later with UUID types)

-- Colaboraciones relationships
ALTER TABLE IF EXISTS detalle_colaboracion DROP CONSTRAINT IF EXISTS fk_detalle_colaboracion_colaboracion;
ALTER TABLE IF EXISTS detalle_colaboracion DROP CONSTRAINT IF EXISTS fk_detalle_colaboracion_tipo;
ALTER TABLE IF EXISTS colaboraciones DROP CONSTRAINT IF EXISTS fk_colaboracion_miembro;

-- Miembros relationships
ALTER TABLE IF EXISTS colaboraciones DROP CONSTRAINT IF EXISTS colaboraciones_miembro_id_fkey;
ALTER TABLE IF EXISTS miembros DROP CONSTRAINT IF EXISTS fk_miembro_grupo_trabajo;
ALTER TABLE IF EXISTS asistencia_culto DROP CONSTRAINT IF EXISTS fk_asistencia_miembro;

-- Prestamos relationships
ALTER TABLE IF EXISTS prestamos DROP CONSTRAINT IF EXISTS fk_prestamo_miembro;
ALTER TABLE IF EXISTS pagos_prestamo DROP CONSTRAINT IF EXISTS fk_pago_prestamo;

-- Inventory relationships
ALTER TABLE IF EXISTS existencias DROP CONSTRAINT IF EXISTS fk_existencia_articulo;
ALTER TABLE IF EXISTS existencias DROP CONSTRAINT IF EXISTS fk_existencia_ubicacion;
ALTER TABLE IF EXISTS movimientos_inventario DROP CONSTRAINT IF EXISTS fk_movimiento_articulo;
ALTER TABLE IF EXISTS movimientos_inventario DROP CONSTRAINT IF EXISTS fk_movimiento_ubicacion_origen;
ALTER TABLE IF EXISTS movimientos_inventario DROP CONSTRAINT IF EXISTS fk_movimiento_ubicacion_destino;
ALTER TABLE IF EXISTS movimientos_inventario DROP CONSTRAINT IF EXISTS fk_movimiento_usuario;

-- Contabilidad relationships
ALTER TABLE IF EXISTS movimiento_general DROP CONSTRAINT IF EXISTS fk_movimiento_categoria_ingreso;
ALTER TABLE IF EXISTS movimiento_general DROP CONSTRAINT IF EXISTS fk_movimiento_categoria_egreso;
ALTER TABLE IF EXISTS movimiento_general DROP CONSTRAINT IF EXISTS fk_movimiento_reporte_mensual;
ALTER TABLE IF EXISTS movimiento_general_adjunto DROP CONSTRAINT IF EXISTS fk_adjunto_movimiento;
ALTER TABLE IF EXISTS contabilidad_registro DROP CONSTRAINT IF EXISTS fk_contabilidad_reporte;

-- =====================================================
-- STEP 3: Add UUID columns and migrate data
-- =====================================================

-- GRUPOS_TRABAJO
ALTER TABLE grupos_trabajo ADD COLUMN id_uuid UUID DEFAULT uuid_generate_v4();
UPDATE grupos_trabajo SET id_uuid = uuid_generate_v4() WHERE id_uuid IS NULL;
ALTER TABLE grupos_trabajo ALTER COLUMN id_uuid SET NOT NULL;

-- MIEMBROS
ALTER TABLE miembros ADD COLUMN id_uuid UUID DEFAULT uuid_generate_v4();
ALTER TABLE miembros ADD COLUMN grupo_trabajo_id_uuid UUID;
UPDATE miembros SET id_uuid = uuid_generate_v4() WHERE id_uuid IS NULL;
ALTER TABLE miembros ALTER COLUMN id_uuid SET NOT NULL;

-- Update foreign key references
UPDATE miembros m
SET grupo_trabajo_id_uuid = gt.id_uuid
FROM grupos_trabajo gt
WHERE m.grupo_trabajo_id = gt.id;

-- TIPOS_COLABORACION
ALTER TABLE tipos_colaboracion ADD COLUMN id_uuid UUID DEFAULT uuid_generate_v4();
UPDATE tipos_colaboracion SET id_uuid = uuid_generate_v4() WHERE id_uuid IS NULL;
ALTER TABLE tipos_colaboracion ALTER COLUMN id_uuid SET NOT NULL;

-- COLABORACIONES
ALTER TABLE colaboraciones ADD COLUMN id_uuid UUID DEFAULT uuid_generate_v4();
ALTER TABLE colaboraciones ADD COLUMN miembro_id_uuid UUID;
ALTER TABLE colaboraciones ADD COLUMN updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP;
UPDATE colaboraciones SET id_uuid = uuid_generate_v4() WHERE id_uuid IS NULL;
ALTER TABLE colaboraciones ALTER COLUMN id_uuid SET NOT NULL;

-- Update foreign key references
UPDATE colaboraciones c
SET miembro_id_uuid = m.id_uuid
FROM miembros m
WHERE c.miembro_id = m.id;

-- DETALLE_COLABORACION
ALTER TABLE detalle_colaboracion ADD COLUMN id_uuid UUID DEFAULT uuid_generate_v4();
ALTER TABLE detalle_colaboracion ADD COLUMN colaboracion_id_uuid UUID;
ALTER TABLE detalle_colaboracion ADD COLUMN tipo_colaboracion_id_uuid UUID;
UPDATE detalle_colaboracion SET id_uuid = uuid_generate_v4() WHERE id_uuid IS NULL;
ALTER TABLE detalle_colaboracion ALTER COLUMN id_uuid SET NOT NULL;

-- Update foreign key references
UPDATE detalle_colaboracion dc
SET colaboracion_id_uuid = c.id_uuid
FROM colaboraciones c
WHERE dc.colaboracion_id = c.id;

UPDATE detalle_colaboracion dc
SET tipo_colaboracion_id_uuid = tc.id_uuid
FROM tipos_colaboracion tc
WHERE dc.tipo_colaboracion_id = tc.id;

-- PRESTAMOS
ALTER TABLE prestamos ADD COLUMN id_uuid UUID DEFAULT uuid_generate_v4();
ALTER TABLE prestamos ADD COLUMN miembro_id_uuid UUID;
UPDATE prestamos SET id_uuid = uuid_generate_v4() WHERE id_uuid IS NULL;
ALTER TABLE prestamos ALTER COLUMN id_uuid SET NOT NULL;

UPDATE prestamos p
SET miembro_id_uuid = m.id_uuid
FROM miembros m
WHERE p.miembro_id = m.id;

-- PAGOS_PRESTAMO
ALTER TABLE pagos_prestamo ADD COLUMN id_uuid UUID DEFAULT uuid_generate_v4();
ALTER TABLE pagos_prestamo ADD COLUMN prestamo_id_uuid UUID;
UPDATE pagos_prestamo SET id_uuid = uuid_generate_v4() WHERE id_uuid IS NULL;
ALTER TABLE pagos_prestamo ALTER COLUMN id_uuid SET NOT NULL;

UPDATE pagos_prestamo pp
SET prestamo_id_uuid = p.id_uuid
FROM prestamos p
WHERE pp.prestamo_id = p.id;

-- ASISTENCIA_CULTO
ALTER TABLE asistencia_culto ADD COLUMN id_uuid UUID DEFAULT uuid_generate_v4();
ALTER TABLE asistencia_culto ADD COLUMN miembro_id_uuid UUID;
UPDATE asistencia_culto SET id_uuid = uuid_generate_v4() WHERE id_uuid IS NULL;
ALTER TABLE asistencia_culto ALTER COLUMN id_uuid SET NOT NULL;

UPDATE asistencia_culto ac
SET miembro_id_uuid = m.id_uuid
FROM miembros m
WHERE ac.miembro_id = m.id;

-- USUARIOS
ALTER TABLE usuarios ADD COLUMN id_uuid UUID DEFAULT uuid_generate_v4();
UPDATE usuarios SET id_uuid = uuid_generate_v4() WHERE id_uuid IS NULL;
ALTER TABLE usuarios ALTER COLUMN id_uuid SET NOT NULL;

-- OFERNDAS
ALTER TABLE oferndas ADD COLUMN id_uuid UUID DEFAULT uuid_generate_v4();
UPDATE oferndas SET id_uuid = uuid_generate_v4() WHERE id_uuid IS NULL;
ALTER TABLE oferndas ALTER COLUMN id_uuid SET NOT NULL;

-- CATEGORIA_INGRESO
ALTER TABLE categoria_ingreso ADD COLUMN id_uuid UUID DEFAULT uuid_generate_v4();
UPDATE categoria_ingreso SET id_uuid = uuid_generate_v4() WHERE id_uuid IS NULL;
ALTER TABLE categoria_ingreso ALTER COLUMN id_uuid SET NOT NULL;

-- CATEGORIA_EGRESO
ALTER TABLE categoria_egreso ADD COLUMN id_uuid UUID DEFAULT uuid_generate_v4();
UPDATE categoria_egreso SET id_uuid = uuid_generate_v4() WHERE id_uuid IS NULL;
ALTER TABLE categoria_egreso ALTER COLUMN id_uuid SET NOT NULL;

-- REPORTE_MENSUAL_GENERAL
ALTER TABLE reporte_mensual_general ADD COLUMN id_uuid UUID DEFAULT uuid_generate_v4();
UPDATE reporte_mensual_general SET id_uuid = uuid_generate_v4() WHERE id_uuid IS NULL;
ALTER TABLE reporte_mensual_general ALTER COLUMN id_uuid SET NOT NULL;

-- REPORTE_MENSUAL_CONTABLE
ALTER TABLE reporte_mensual_contable ADD COLUMN id_uuid UUID DEFAULT uuid_generate_v4();
UPDATE reporte_mensual_contable SET id_uuid = uuid_generate_v4() WHERE id_uuid IS NULL;
ALTER TABLE reporte_mensual_contable ALTER COLUMN id_uuid SET NOT NULL;

-- MOVIMIENTO_GENERAL
ALTER TABLE movimiento_general ADD COLUMN id_uuid UUID DEFAULT uuid_generate_v4();
ALTER TABLE movimiento_general ADD COLUMN categoria_ingreso_id_uuid UUID;
ALTER TABLE movimiento_general ADD COLUMN categoria_egreso_id_uuid UUID;
ALTER TABLE movimiento_general ADD COLUMN reporte_mensual_id_uuid UUID;
UPDATE movimiento_general SET id_uuid = uuid_generate_v4() WHERE id_uuid IS NULL;
ALTER TABLE movimiento_general ALTER COLUMN id_uuid SET NOT NULL;

UPDATE movimiento_general mg
SET categoria_ingreso_id_uuid = ci.id_uuid
FROM categoria_ingreso ci
WHERE mg.categoria_ingreso_id = ci.id;

UPDATE movimiento_general mg
SET categoria_egreso_id_uuid = ce.id_uuid
FROM categoria_egreso ce
WHERE mg.categoria_egreso_id = ce.id;

UPDATE movimiento_general mg
SET reporte_mensual_id_uuid = rm.id_uuid
FROM reporte_mensual_general rm
WHERE mg.reporte_mensual_id = rm.id;

-- MOVIMIENTO_GENERAL_ADJUNTO
ALTER TABLE movimiento_general_adjunto ADD COLUMN id_uuid UUID DEFAULT uuid_generate_v4();
ALTER TABLE movimiento_general_adjunto ADD COLUMN movimiento_id_uuid UUID;
UPDATE movimiento_general_adjunto SET id_uuid = uuid_generate_v4() WHERE id_uuid IS NULL;
ALTER TABLE movimiento_general_adjunto ALTER COLUMN id_uuid SET NOT NULL;

UPDATE movimiento_general_adjunto mga
SET movimiento_id_uuid = mg.id_uuid
FROM movimiento_general mg
WHERE mga.movimiento_id = mg.id;

-- CONTABILIDAD_REGISTRO
ALTER TABLE contabilidad_registro ADD COLUMN id_uuid UUID DEFAULT uuid_generate_v4();
ALTER TABLE contabilidad_registro ADD COLUMN reporte_id_uuid UUID;
UPDATE contabilidad_registro SET id_uuid = uuid_generate_v4() WHERE id_uuid IS NULL;
ALTER TABLE contabilidad_registro ALTER COLUMN id_uuid SET NOT NULL;

UPDATE contabilidad_registro cr
SET reporte_id_uuid = rm.id_uuid
FROM reporte_mensual_contable rm
WHERE cr.reporte_id = rm.id;

-- ARTICULOS (assuming you have this table)
ALTER TABLE IF EXISTS articulos ADD COLUMN id_uuid UUID DEFAULT uuid_generate_v4();
UPDATE articulos SET id_uuid = uuid_generate_v4() WHERE id_uuid IS NULL;
ALTER TABLE IF EXISTS articulos ALTER COLUMN id_uuid SET NOT NULL;

-- UBICACIONES (assuming you have this table)
ALTER TABLE IF EXISTS ubicaciones ADD COLUMN id_uuid UUID DEFAULT uuid_generate_v4();
UPDATE ubicaciones SET id_uuid = uuid_generate_v4() WHERE id_uuid IS NULL;
ALTER TABLE IF EXISTS ubicaciones ALTER COLUMN id_uuid SET NOT NULL;

-- EXISTENCIAS
ALTER TABLE IF EXISTS existencias ADD COLUMN id_uuid UUID DEFAULT uuid_generate_v4();
ALTER TABLE IF EXISTS existencias ADD COLUMN articulo_id_uuid UUID;
ALTER TABLE IF EXISTS existencias ADD COLUMN ubicacion_id_uuid UUID;
UPDATE existencias SET id_uuid = uuid_generate_v4() WHERE id_uuid IS NULL;
ALTER TABLE IF EXISTS existencias ALTER COLUMN id_uuid SET NOT NULL;

UPDATE existencias e
SET articulo_id_uuid = a.id_uuid
FROM articulos a
WHERE e.articulo_id = a.id;

UPDATE existencias e
SET ubicacion_id_uuid = u.id_uuid
FROM ubicaciones u
WHERE e.ubicacion_id = u.id;

-- MOVIMIENTOS_INVENTARIO
ALTER TABLE IF EXISTS movimientos_inventario ADD COLUMN id_uuid UUID DEFAULT uuid_generate_v4();
ALTER TABLE IF EXISTS movimientos_inventario ADD COLUMN articulo_id_uuid UUID;
ALTER TABLE IF EXISTS movimientos_inventario ADD COLUMN ubicacion_origen_id_uuid UUID;
ALTER TABLE IF EXISTS movimientos_inventario ADD COLUMN ubicacion_destino_id_uuid UUID;
ALTER TABLE IF EXISTS movimientos_inventario ADD COLUMN usuario_id_uuid UUID;
UPDATE movimientos_inventario SET id_uuid = uuid_generate_v4() WHERE id_uuid IS NULL;
ALTER TABLE IF EXISTS movimientos_inventario ALTER COLUMN id_uuid SET NOT NULL;

UPDATE movimientos_inventario mi
SET articulo_id_uuid = a.id_uuid
FROM articulos a
WHERE mi.articulo_id = a.id;

UPDATE movimientos_inventario mi
SET ubicacion_origen_id_uuid = u.id_uuid
FROM ubicaciones u
WHERE mi.ubicacion_origen_id = u.id;

UPDATE movimientos_inventario mi
SET ubicacion_destino_id_uuid = u.id_uuid
FROM ubicaciones u
WHERE mi.ubicacion_destino_id = u.id;

UPDATE movimientos_inventario mi
SET usuario_id_uuid = usr.id_uuid
FROM usuarios usr
WHERE mi.usuario_id = usr.id;

-- =====================================================
-- STEP 4: Drop old ID columns and rename UUID columns
-- =====================================================

-- GRUPOS_TRABAJO
ALTER TABLE grupos_trabajo DROP CONSTRAINT IF EXISTS grupos_trabajo_pkey;
ALTER TABLE grupos_trabajo DROP COLUMN id;
ALTER TABLE grupos_trabajo RENAME COLUMN id_uuid TO id;
ALTER TABLE grupos_trabajo ADD PRIMARY KEY (id);

-- MIEMBROS
ALTER TABLE miembros DROP CONSTRAINT IF EXISTS miembros_pkey;
ALTER TABLE miembros DROP COLUMN id;
ALTER TABLE miembros DROP COLUMN grupo_trabajo_id;
ALTER TABLE miembros RENAME COLUMN id_uuid TO id;
ALTER TABLE miembros RENAME COLUMN grupo_trabajo_id_uuid TO grupo_trabajo_id;
ALTER TABLE miembros ADD PRIMARY KEY (id);

-- TIPOS_COLABORACION
ALTER TABLE tipos_colaboracion DROP CONSTRAINT IF EXISTS tipos_colaboracion_pkey;
ALTER TABLE tipos_colaboracion DROP COLUMN id;
ALTER TABLE tipos_colaboracion RENAME COLUMN id_uuid TO id;
ALTER TABLE tipos_colaboracion ADD PRIMARY KEY (id);

-- COLABORACIONES
ALTER TABLE colaboraciones DROP CONSTRAINT IF EXISTS colaboraciones_pkey;
ALTER TABLE colaboraciones DROP COLUMN id;
ALTER TABLE colaboraciones DROP COLUMN miembro_id;
ALTER TABLE colaboraciones RENAME COLUMN id_uuid TO id;
ALTER TABLE colaboraciones RENAME COLUMN miembro_id_uuid TO miembro_id;
ALTER TABLE colaboraciones ADD PRIMARY KEY (id);

-- DETALLE_COLABORACION
ALTER TABLE detalle_colaboracion DROP CONSTRAINT IF EXISTS detalle_colaboracion_pkey;
ALTER TABLE detalle_colaboracion DROP COLUMN id;
ALTER TABLE detalle_colaboracion DROP COLUMN colaboracion_id;
ALTER TABLE detalle_colaboracion DROP COLUMN tipo_colaboracion_id;
ALTER TABLE detalle_colaboracion RENAME COLUMN id_uuid TO id;
ALTER TABLE detalle_colaboracion RENAME COLUMN colaboracion_id_uuid TO colaboracion_id;
ALTER TABLE detalle_colaboracion RENAME COLUMN tipo_colaboracion_id_uuid TO tipo_colaboracion_id;
ALTER TABLE detalle_colaboracion ADD PRIMARY KEY (id);

-- PRESTAMOS
ALTER TABLE prestamos DROP CONSTRAINT IF EXISTS prestamos_pkey;
ALTER TABLE prestamos DROP COLUMN id;
ALTER TABLE prestamos DROP COLUMN miembro_id;
ALTER TABLE prestamos RENAME COLUMN id_uuid TO id;
ALTER TABLE prestamos RENAME COLUMN miembro_id_uuid TO miembro_id;
ALTER TABLE prestamos ADD PRIMARY KEY (id);

-- PAGOS_PRESTAMO
ALTER TABLE pagos_prestamo DROP CONSTRAINT IF EXISTS pagos_prestamo_pkey;
ALTER TABLE pagos_prestamo DROP COLUMN id;
ALTER TABLE pagos_prestamo DROP COLUMN prestamo_id;
ALTER TABLE pagos_prestamo RENAME COLUMN id_uuid TO id;
ALTER TABLE pagos_prestamo RENAME COLUMN prestamo_id_uuid TO prestamo_id;
ALTER TABLE pagos_prestamo ADD PRIMARY KEY (id);

-- ASISTENCIA_CULTO
ALTER TABLE asistencia_culto DROP CONSTRAINT IF EXISTS asistencia_culto_pkey;
ALTER TABLE asistencia_culto DROP COLUMN id;
ALTER TABLE asistencia_culto DROP COLUMN miembro_id;
ALTER TABLE asistencia_culto RENAME COLUMN id_uuid TO id;
ALTER TABLE asistencia_culto RENAME COLUMN miembro_id_uuid TO miembro_id;
ALTER TABLE asistencia_culto ADD PRIMARY KEY (id);

-- USUARIOS
ALTER TABLE usuarios DROP CONSTRAINT IF EXISTS usuarios_pkey;
ALTER TABLE usuarios DROP COLUMN id;
ALTER TABLE usuarios RENAME COLUMN id_uuid TO id;
ALTER TABLE usuarios ADD PRIMARY KEY (id);

-- OFERNDAS
ALTER TABLE oferndas DROP CONSTRAINT IF EXISTS oferndas_pkey;
ALTER TABLE oferndas DROP COLUMN id;
ALTER TABLE oferndas RENAME COLUMN id_uuid TO id;
ALTER TABLE oferndas ADD PRIMARY KEY (id);

-- CATEGORIA_INGRESO
ALTER TABLE categoria_ingreso DROP CONSTRAINT IF EXISTS categoria_ingreso_pkey;
ALTER TABLE categoria_ingreso DROP COLUMN id;
ALTER TABLE categoria_ingreso RENAME COLUMN id_uuid TO id;
ALTER TABLE categoria_ingreso ADD PRIMARY KEY (id);

-- CATEGORIA_EGRESO
ALTER TABLE categoria_egreso DROP CONSTRAINT IF EXISTS categoria_egreso_pkey;
ALTER TABLE categoria_egreso DROP COLUMN id;
ALTER TABLE categoria_egreso RENAME COLUMN id_uuid TO id;
ALTER TABLE categoria_egreso ADD PRIMARY KEY (id);

-- REPORTE_MENSUAL_GENERAL
ALTER TABLE reporte_mensual_general DROP CONSTRAINT IF EXISTS reporte_mensual_general_pkey;
ALTER TABLE reporte_mensual_general DROP COLUMN id;
ALTER TABLE reporte_mensual_general RENAME COLUMN id_uuid TO id;
ALTER TABLE reporte_mensual_general ADD PRIMARY KEY (id);

-- REPORTE_MENSUAL_CONTABLE
ALTER TABLE reporte_mensual_contable DROP CONSTRAINT IF EXISTS reporte_mensual_contable_pkey;
ALTER TABLE reporte_mensual_contable DROP COLUMN id;
ALTER TABLE reporte_mensual_contable RENAME COLUMN id_uuid TO id;
ALTER TABLE reporte_mensual_contable ADD PRIMARY KEY (id);

-- MOVIMIENTO_GENERAL
ALTER TABLE movimiento_general DROP CONSTRAINT IF EXISTS movimiento_general_pkey;
ALTER TABLE movimiento_general DROP COLUMN id;
ALTER TABLE movimiento_general DROP COLUMN categoria_ingreso_id;
ALTER TABLE movimiento_general DROP COLUMN categoria_egreso_id;
ALTER TABLE movimiento_general DROP COLUMN reporte_mensual_id;
ALTER TABLE movimiento_general RENAME COLUMN id_uuid TO id;
ALTER TABLE movimiento_general RENAME COLUMN categoria_ingreso_id_uuid TO categoria_ingreso_id;
ALTER TABLE movimiento_general RENAME COLUMN categoria_egreso_id_uuid TO categoria_egreso_id;
ALTER TABLE movimiento_general RENAME COLUMN reporte_mensual_id_uuid TO reporte_mensual_id;
ALTER TABLE movimiento_general ADD PRIMARY KEY (id);

-- MOVIMIENTO_GENERAL_ADJUNTO
ALTER TABLE movimiento_general_adjunto DROP CONSTRAINT IF EXISTS movimiento_general_adjunto_pkey;
ALTER TABLE movimiento_general_adjunto DROP COLUMN id;
ALTER TABLE movimiento_general_adjunto DROP COLUMN movimiento_id;
ALTER TABLE movimiento_general_adjunto RENAME COLUMN id_uuid TO id;
ALTER TABLE movimiento_general_adjunto RENAME COLUMN movimiento_id_uuid TO movimiento_id;
ALTER TABLE movimiento_general_adjunto ADD PRIMARY KEY (id);

-- CONTABILIDAD_REGISTRO
ALTER TABLE contabilidad_registro DROP CONSTRAINT IF EXISTS contabilidad_registro_pkey;
ALTER TABLE contabilidad_registro DROP COLUMN id;
ALTER TABLE contabilidad_registro DROP COLUMN reporte_id;
ALTER TABLE contabilidad_registro RENAME COLUMN id_uuid TO id;
ALTER TABLE contabilidad_registro RENAME COLUMN reporte_id_uuid TO reporte_id;
ALTER TABLE contabilidad_registro ADD PRIMARY KEY (id);

-- ARTICULOS
ALTER TABLE IF EXISTS articulos DROP CONSTRAINT IF EXISTS articulos_pkey;
ALTER TABLE IF EXISTS articulos DROP COLUMN id;
ALTER TABLE IF EXISTS articulos RENAME COLUMN id_uuid TO id;
ALTER TABLE IF EXISTS articulos ADD PRIMARY KEY (id);

-- UBICACIONES
ALTER TABLE IF EXISTS ubicaciones DROP CONSTRAINT IF EXISTS ubicaciones_pkey;
ALTER TABLE IF EXISTS ubicaciones DROP COLUMN id;
ALTER TABLE IF EXISTS ubicaciones RENAME COLUMN id_uuid TO id;
ALTER TABLE IF EXISTS ubicaciones ADD PRIMARY KEY (id);

-- EXISTENCIAS
ALTER TABLE IF EXISTS existencias DROP CONSTRAINT IF EXISTS existencias_pkey;
ALTER TABLE IF EXISTS existencias DROP COLUMN id;
ALTER TABLE IF EXISTS existencias DROP COLUMN articulo_id;
ALTER TABLE IF EXISTS existencias DROP COLUMN ubicacion_id;
ALTER TABLE IF EXISTS existencias RENAME COLUMN id_uuid TO id;
ALTER TABLE IF EXISTS existencias RENAME COLUMN articulo_id_uuid TO articulo_id;
ALTER TABLE IF EXISTS existencias RENAME COLUMN ubicacion_id_uuid TO ubicacion_id;
ALTER TABLE IF EXISTS existencias ADD PRIMARY KEY (id);

-- MOVIMIENTOS_INVENTARIO
ALTER TABLE IF EXISTS movimientos_inventario DROP CONSTRAINT IF EXISTS movimientos_inventario_pkey;
ALTER TABLE IF EXISTS movimientos_inventario DROP COLUMN id;
ALTER TABLE IF EXISTS movimientos_inventario DROP COLUMN articulo_id;
ALTER TABLE IF EXISTS movimientos_inventario DROP COLUMN ubicacion_origen_id;
ALTER TABLE IF EXISTS movimientos_inventario DROP COLUMN ubicacion_destino_id;
ALTER TABLE IF EXISTS movimientos_inventario DROP COLUMN usuario_id;
ALTER TABLE IF EXISTS movimientos_inventario RENAME COLUMN id_uuid TO id;
ALTER TABLE IF EXISTS movimientos_inventario RENAME COLUMN articulo_id_uuid TO articulo_id;
ALTER TABLE IF EXISTS movimientos_inventario RENAME COLUMN ubicacion_origen_id_uuid TO ubicacion_origen_id;
ALTER TABLE IF EXISTS movimientos_inventario RENAME COLUMN ubicacion_destino_id_uuid TO ubicacion_destino_id;
ALTER TABLE IF EXISTS movimientos_inventario RENAME COLUMN usuario_id_uuid TO usuario_id;
ALTER TABLE IF EXISTS movimientos_inventario ADD PRIMARY KEY (id);

-- =====================================================
-- STEP 5: Recreate foreign key constraints
-- =====================================================

-- Miembros -> Grupos_trabajo
ALTER TABLE miembros
    ADD CONSTRAINT fk_miembro_grupo_trabajo 
    FOREIGN KEY (grupo_trabajo_id) 
    REFERENCES grupos_trabajo(id) 
    ON DELETE SET NULL;

-- Colaboraciones -> Miembros
ALTER TABLE colaboraciones
    ADD CONSTRAINT fk_colaboracion_miembro 
    FOREIGN KEY (miembro_id) 
    REFERENCES miembros(id) 
    ON DELETE CASCADE;

-- Detalle_colaboracion -> Colaboraciones
ALTER TABLE detalle_colaboracion
    ADD CONSTRAINT fk_detalle_colaboracion_colaboracion 
    FOREIGN KEY (colaboracion_id) 
    REFERENCES colaboraciones(id) 
    ON DELETE CASCADE;

-- Detalle_colaboracion -> Tipos_colaboracion
ALTER TABLE detalle_colaboracion
    ADD CONSTRAINT fk_detalle_colaboracion_tipo 
    FOREIGN KEY (tipo_colaboracion_id) 
    REFERENCES tipos_colaboracion(id) 
    ON DELETE CASCADE;

-- Prestamos -> Miembros
ALTER TABLE prestamos
    ADD CONSTRAINT fk_prestamo_miembro 
    FOREIGN KEY (miembro_id) 
    REFERENCES miembros(id) 
    ON DELETE CASCADE;

-- Pagos_prestamo -> Prestamos
ALTER TABLE pagos_prestamo
    ADD CONSTRAINT fk_pago_prestamo 
    FOREIGN KEY (prestamo_id) 
    REFERENCES prestamos(id) 
    ON DELETE CASCADE;

-- Asistencia_culto -> Miembros
ALTER TABLE asistencia_culto
    ADD CONSTRAINT fk_asistencia_miembro 
    FOREIGN KEY (miembro_id) 
    REFERENCES miembros(id) 
    ON DELETE CASCADE;

-- Movimiento_general -> Categoria_ingreso
ALTER TABLE movimiento_general
    ADD CONSTRAINT fk_movimiento_categoria_ingreso 
    FOREIGN KEY (categoria_ingreso_id) 
    REFERENCES categoria_ingreso(id) 
    ON DELETE SET NULL;

-- Movimiento_general -> Categoria_egreso
ALTER TABLE movimiento_general
    ADD CONSTRAINT fk_movimiento_categoria_egreso 
    FOREIGN KEY (categoria_egreso_id) 
    REFERENCES categoria_egreso(id) 
    ON DELETE SET NULL;

-- Movimiento_general -> Reporte_mensual_general
ALTER TABLE movimiento_general
    ADD CONSTRAINT fk_movimiento_reporte_mensual 
    FOREIGN KEY (reporte_mensual_id) 
    REFERENCES reporte_mensual_general(id) 
    ON DELETE CASCADE;

-- Movimiento_general_adjunto -> Movimiento_general
ALTER TABLE movimiento_general_adjunto
    ADD CONSTRAINT fk_adjunto_movimiento 
    FOREIGN KEY (movimiento_id) 
    REFERENCES movimiento_general(id) 
    ON DELETE CASCADE;

-- Contabilidad_registro -> Reporte_mensual_contable
ALTER TABLE contabilidad_registro
    ADD CONSTRAINT fk_contabilidad_reporte 
    FOREIGN KEY (reporte_id) 
    REFERENCES reporte_mensual_contable(id) 
    ON DELETE CASCADE;

-- Existencias -> Articulos
ALTER TABLE IF EXISTS existencias
    ADD CONSTRAINT fk_existencia_articulo 
    FOREIGN KEY (articulo_id) 
    REFERENCES articulos(id) 
    ON DELETE CASCADE;

-- Existencias -> Ubicaciones
ALTER TABLE IF EXISTS existencias
    ADD CONSTRAINT fk_existencia_ubicacion 
    FOREIGN KEY (ubicacion_id) 
    REFERENCES ubicaciones(id) 
    ON DELETE CASCADE;

-- Movimientos_inventario -> Articulos
ALTER TABLE IF EXISTS movimientos_inventario
    ADD CONSTRAINT fk_movimiento_articulo 
    FOREIGN KEY (articulo_id) 
    REFERENCES articulos(id) 
    ON DELETE CASCADE;

-- Movimientos_inventario -> Ubicaciones (origen)
ALTER TABLE IF EXISTS movimientos_inventario
    ADD CONSTRAINT fk_movimiento_ubicacion_origen 
    FOREIGN KEY (ubicacion_origen_id) 
    REFERENCES ubicaciones(id) 
    ON DELETE SET NULL;

-- Movimientos_inventario -> Ubicaciones (destino)
ALTER TABLE IF EXISTS movimientos_inventario
    ADD CONSTRAINT fk_movimiento_ubicacion_destino 
    FOREIGN KEY (ubicacion_destino_id) 
    REFERENCES ubicaciones(id) 
    ON DELETE SET NULL;

-- Movimientos_inventario -> Usuarios
ALTER TABLE IF EXISTS movimientos_inventario
    ADD CONSTRAINT fk_movimiento_usuario 
    FOREIGN KEY (usuario_id) 
    REFERENCES usuarios(id) 
    ON DELETE SET NULL;

-- =====================================================
-- STEP 6: Create indexes for better performance
-- =====================================================

CREATE INDEX IF NOT EXISTS idx_colaboraciones_miembro_id ON colaboraciones(miembro_id);
CREATE INDEX IF NOT EXISTS idx_detalle_colaboracion_id ON detalle_colaboracion(colaboracion_id);
CREATE INDEX IF NOT EXISTS idx_detalle_tipo_id ON detalle_colaboracion(tipo_colaboracion_id);
CREATE INDEX IF NOT EXISTS idx_colaboraciones_updated_at ON colaboraciones(updated_at);
CREATE INDEX IF NOT EXISTS idx_prestamos_miembro_id ON prestamos(miembro_id);
CREATE INDEX IF NOT EXISTS idx_asistencia_miembro_id ON asistencia_culto(miembro_id);
CREATE INDEX IF NOT EXISTS idx_movimiento_reporte_id ON movimiento_general(reporte_mensual_id);

-- =====================================================
-- VERIFICATION QUERIES
-- =====================================================
-- Run these after migration to verify success:

-- SELECT 'colaboraciones' as table_name, COUNT(*) as count FROM colaboraciones;
-- SELECT 'miembros' as table_name, COUNT(*) as count FROM miembros;
-- SELECT 'detalle_colaboracion' as table_name, COUNT(*) as count FROM detalle_colaboracion;
-- 
-- -- Check that all IDs are now UUIDs:
-- SELECT id, miembro_id FROM colaboraciones LIMIT 5;
-- SELECT id, grupo_trabajo_id FROM miembros LIMIT 5;

-- =====================================================
-- COMMIT or ROLLBACK
-- =====================================================
-- If everything looks good, COMMIT:
COMMIT;

-- If there are errors, ROLLBACK:
-- ROLLBACK;

-- =====================================================
-- POST-MIGRATION NOTES
-- =====================================================
-- After running this script successfully:
-- 1. Update your C# models to use Guid instead of long
-- 2. Rebuild your application
-- 3. Test thoroughly before deploying to production
-- 4. Monitor for any issues with existing records
-- =====================================================
