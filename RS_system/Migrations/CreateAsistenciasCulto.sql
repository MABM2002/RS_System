-- SQL para crear la tabla de asistencias de culto
-- PostgreSQL

CREATE TABLE IF NOT EXISTS asistencias_culto (
    id BIGSERIAL PRIMARY KEY,
    fecha_hora_inicio TIMESTAMP WITH TIME ZONE NOT NULL,
    tipo_culto INTEGER NOT NULL,
    tipo_conteo INTEGER NOT NULL,
    
    -- Campos para TipoConteo.Detallado
    hermanas_misioneras INTEGER,
    hermanos_fraternidad INTEGER,
    embajadores_cristo INTEGER,
    ninos INTEGER,
    visitas INTEGER,
    amigos INTEGER,
    
    -- Campos para TipoConteo.General
    adultos_general INTEGER,
    
    -- Campo para TipoConteo.Total
    total_manual INTEGER,
    
    -- Observaciones y auditoría
    observaciones VARCHAR(500),
    creado_por VARCHAR(100),
    creado_en TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    actualizado_en TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Índices para mejorar rendimiento en búsquedas frecuentes
CREATE INDEX idx_asistencias_culto_fecha ON asistencias_culto(fecha_hora_inicio DESC);
CREATE INDEX idx_asistencias_culto_tipo_culto ON asistencias_culto(tipo_culto);
CREATE INDEX idx_asistencias_culto_tipo_conteo ON asistencias_culto(tipo_conteo);

-- Comentarios para documentación
COMMENT ON TABLE asistencias_culto IS 'Registro de asistencia de cultos y actividades eclesiásticas';
COMMENT ON COLUMN asistencias_culto.tipo_culto IS '1=Matutinos, 2=Dominicales, 3=Generales, 4=ConcilioMisionero, 5=Fraternidad, 6=Embajadores, 7=AccionDeGracias, 8=CampanasEvangelisticas, 9=CultosEspeciales, 10=Vigilias, 11=Velas';
COMMENT ON COLUMN asistencias_culto.tipo_conteo IS '1=Detallado, 2=General, 3=Total';
COMMENT ON COLUMN asistencias_culto.hermanas_misioneras IS 'Hermanas del Concilio Misionero Femenil (conteo detallado)';
COMMENT ON COLUMN asistencias_culto.hermanos_fraternidad IS 'Hermanos de Fraternidad de Varones (conteo detallado)';
COMMENT ON COLUMN asistencias_culto.embajadores_cristo IS 'Embajadores de Cristo (conteo detallado)';
COMMENT ON COLUMN asistencias_culto.ninos IS 'Niños (usado en detallado y general)';
COMMENT ON COLUMN asistencias_culto.visitas IS 'Visitas (conteo detallado)';
COMMENT ON COLUMN asistencias_culto.amigos IS 'Amigos (conteo detallado)';
COMMENT ON COLUMN asistencias_culto.adultos_general IS 'Adultos en general (conteo general)';
COMMENT ON COLUMN asistencias_culto.total_manual IS 'Total directo (conteo total)';
COMMENT ON COLUMN asistencias_culto.observaciones IS 'Observaciones adicionales sobre el culto';
COMMENT ON COLUMN asistencias_culto.creado_por IS 'Usuario que registró la asistencia';

-- Opcional: Crear una vista para facilitar consultas con total calculado
CREATE OR REPLACE VIEW vw_asistencias_culto AS
SELECT 
    id,
    fecha_hora_inicio,
    tipo_culto,
    tipo_conteo,
    hermanas_misioneras,
    hermanos_fraternidad,
    embajadores_cristo,
    ninos,
    visitas,
    amigos,
    adultos_general,
    total_manual,
    observaciones,
    creado_por,
    creado_en,
    actualizado_en,
    CASE tipo_conteo
        WHEN 1 THEN -- Detallado
            COALESCE(hermanas_misioneras, 0) + 
            COALESCE(hermanos_fraternidad, 0) + 
            COALESCE(embajadores_cristo, 0) + 
            COALESCE(ninos, 0) + 
            COALESCE(visitas, 0) + 
            COALESCE(amigos, 0)
        WHEN 2 THEN -- General
            COALESCE(adultos_general, 0) + COALESCE(ninos, 0)
        WHEN 3 THEN -- Total
            COALESCE(total_manual, 0)
        ELSE 0
    END AS total_calculado
FROM asistencias_culto;

COMMENT ON VIEW vw_asistencias_culto IS 'Vista de asistencias con total calculado automáticamente';