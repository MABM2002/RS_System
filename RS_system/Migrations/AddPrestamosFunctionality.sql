-- SQL para agregar funcionalidad de préstamos al sistema de inventario
-- PostgreSQL

-- 1. Agregar el nuevo tipo de movimiento PRESTAMO al enum existente
-- Nota: PostgreSQL no tiene ALTER ENUM, así que necesitamos recrear el enum
-- Primero creamos el nuevo enum

CREATE TYPE tipo_movimiento_new AS ENUM (
    'ENTRADA',
    'SALIDA', 
    'TRASLADO',
    'BAJA',
    'REPARACION',
    'AJUSTE',
    'CAMBIO_ESTADO',
    'PRESTAMO'
);

-- Convertimos los datos existentes al nuevo enum
ALTER TABLE movimientos_inventario 
ALTER COLUMN tipo_movimiento TYPE tipo_movimiento_new 
USING tipo_movimiento::text::tipo_movimiento_new;

-- Eliminamos el enum viejo y renombramos el nuevo
DROP TYPE tipo_movimiento;
ALTER TYPE tipo_movimiento_new RENAME TO tipo_movimiento;

-- 2. Crear tabla de préstamos
CREATE TABLE prestamos (
    id BIGSERIAL PRIMARY KEY,
    articulo_id INTEGER NOT NULL,
    cantidad INTEGER NOT NULL,
    persona_nombre VARCHAR(200) NOT NULL,
    persona_identificacion VARCHAR(50),
    fecha_prestamo TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    fecha_devolucion_estimada TIMESTAMP WITH TIME ZONE,
    fecha_devolucion_real TIMESTAMP WITH TIME ZONE,
    estado VARCHAR(20) NOT NULL DEFAULT 'ACTIVO',
    observacion VARCHAR(500),
    usuario_id VARCHAR(100),
    CONSTRAINT fk_prestamos_articulos FOREIGN KEY (articulo_id) REFERENCES articulos(id) ON DELETE CASCADE
);

-- 3. Crear tabla de detalles de préstamo (para códigos individuales)
CREATE TABLE prestamo_detalles (
    id BIGSERIAL PRIMARY KEY,
    prestamo_id BIGINT NOT NULL,
    codigo_articulo_individual VARCHAR(100) NOT NULL,
    estado VARCHAR(20) NOT NULL DEFAULT 'PRESTADO',
    fecha_devolucion TIMESTAMP WITH TIME ZONE,
    observacion VARCHAR(300),
    CONSTRAINT fk_prestamo_detalles_prestamo FOREIGN KEY (prestamo_id) REFERENCES prestamos(id) ON DELETE CASCADE
);

-- 4. Crear índices para mejor rendimiento
CREATE INDEX idx_prestamos_articulo_id ON prestamos(articulo_id);
CREATE INDEX idx_prestamos_estado ON prestamos(estado);
CREATE INDEX idx_prestamos_fecha_prestamo ON prestamos(fecha_prestamo);
CREATE INDEX idx_prestamo_detalles_prestamo_id ON prestamo_detalles(prestamo_id);
CREATE INDEX idx_prestamo_detalles_codigo ON prestamo_detalles(codigo_articulo_individual);

-- 5. Crear función para generar códigos individuales automáticamente
CREATE OR REPLACE FUNCTION generar_codigo_individual(p_codigo_base VARCHAR, p_secuencia INTEGER)
RETURNS VARCHAR AS $$
BEGIN
    RETURN p_codigo_base || '-' || LPAD(p_secuencia::TEXT, 3, '0');
END;
$$ LANGUAGE plpgsql;

-- 6. Crear trigger para actualizar estado de préstamo cuando todos los detalles están devueltos
CREATE OR REPLACE FUNCTION actualizar_estado_prestamo()
RETURNS TRIGGER AS $$
BEGIN
    -- Si se actualiza un detalle a DEVUELTO, verificar si todos están devueltos
    IF NEW.estado = 'DEVUELTO' THEN
        UPDATE prestamos 
        SET estado = CASE 
            WHEN NOT EXISTS (
                SELECT 1 FROM prestamo_detalles 
                WHERE prestamo_id = NEW.prestamo_id AND estado != 'DEVUELTO'
            ) THEN 'DEVUELTO'
            ELSE estado
        END,
        fecha_devolucion_real = CASE 
            WHEN NOT EXISTS (
                SELECT 1 FROM prestamo_detalles 
                WHERE prestamo_id = NEW.prestamo_id AND estado != 'DEVUELTO'
            ) THEN CURRENT_TIMESTAMP
            ELSE fecha_devolucion_real
        END
        WHERE id = NEW.prestamo_id;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 7. Crear el trigger
CREATE TRIGGER trigger_actualizar_estado_prestamo
    AFTER UPDATE ON prestamo_detalles
    FOR EACH ROW
    EXECUTE FUNCTION actualizar_estado_prestamo();

-- 8. Insertar datos de ejemplo (opcional)
-- INSERT INTO prestamos (articulo_id, cantidad, persona_nombre, persona_identificacion, fecha_devolucion_estimada, observacion, usuario_id)
-- VALUES (1, 2, 'Juan Pérez', '12345678', CURRENT_TIMESTAMP + INTERVAL '7 days', 'Préstamo para evento especial', 'admin');

-- Generar códigos individuales para el préstamo de ejemplo
-- INSERT INTO prestamo_detalles (prestamo_id, codigo_articulo_individual, estado)
-- VALUES 
--     (1, generar_codigo_individual('sp-b20', 1), 'PRESTADO'),
--     (1, generar_codigo_individual('sp-b20', 2), 'PRESTADO');

-- 9. Crear vista para préstamos activos con detalles
CREATE OR REPLACE VIEW vista_prestamos_activos AS
SELECT 
    p.id,
    p.articulo_id,
    a.codigo as articulo_codigo,
    a.nombre as articulo_nombre,
    p.cantidad,
    p.persona_nombre,
    p.persona_identificacion,
    p.fecha_prestamo,
    p.fecha_devolucion_estimada,
    p.estado,
    p.observacion,
    p.usuario_id,
    COUNT(pd.id) as detalles_devueltos,
    (p.cantidad - COUNT(pd.id)) as detalles_pendientes
FROM prestamos p
LEFT JOIN articulos a ON p.articulo_id = a.id
LEFT JOIN prestamo_detalles pd ON p.id = pd.prestamo_id AND pd.estado = 'DEVUELTO'
WHERE p.estado = 'ACTIVO'
GROUP BY p.id, p.articulo_id, a.codigo, a.nombre, p.cantidad, p.persona_nombre, p.persona_identificacion, p.fecha_prestamo, p.fecha_devolucion_estimada, p.estado, p.observacion, p.usuario_id;

-- 10. Conceder permisos (ajustar según tu usuario de base de datos)
-- GRANT SELECT, INSERT, UPDATE, DELETE ON prestamos TO tu_usuario;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON prestamo_detalles TO tu_usuario;
-- GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO tu_usuario;
-- GRANT EXECUTE ON FUNCTION generar_codigo_individual TO tu_usuario;
-- GRANT EXECUTE ON FUNCTION actualizar_estado_prestamo TO tu_usuario;
-- GRANT SELECT ON vista_prestamos_activos TO tu_usuario;