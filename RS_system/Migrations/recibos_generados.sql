CREATE TABLE recibos_generados (
    num_recibo TEXT PRIMARY KEY,
    nombre_beneficiario TEXT NOT NULL,
    nombre_iglesia TEXT NOT NULL,
    monto_decimal NUMERIC(15, 2) NOT NULL,
    monto_texto TEXT NOT NULL,
    dia INTEGER NOT NULL,
    mes INTEGER NOT NULL,
    anio INTEGER NOT NULL,
    concepto TEXT,
    id_salida INTEGER NOT NULL,
    fecha_generacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    eliminado BOOLEAN NOT NULL DEFAULT FALSE,
    actualizado_en TIMESTAMP,
    creado_por TEXT NOT NULL,
    CONSTRAINT fk_id_salida FOREIGN KEY (id_salida) REFERENCES diezmo_salidas(id)
);
