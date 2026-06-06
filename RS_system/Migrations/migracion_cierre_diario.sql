-- Migration script for Daily Closing and Accounting Synchronization
-- This script adds the necessary fields for closing ColaboracionHeads

-- Step 1: Add new columns to colaboracion_heads table for closing functionality
ALTER TABLE public.colaboracion_heads 
ADD COLUMN IF NOT EXISTS es_cerrado BOOLEAN NOT NULL DEFAULT FALSE;

ALTER TABLE public.colaboracion_heads 
ADD COLUMN IF NOT EXISTS fecha_cierre TIMESTAMP;

ALTER TABLE public.colaboracion_heads 
ADD COLUMN IF NOT EXISTS cerrado_por VARCHAR(100);

-- Step 2: Create index for better performance when querying closed/open heads
CREATE INDEX IF NOT EXISTS ix_colaboracion_heads_es_cerrado 
ON public.colaboracion_heads (es_cerrado);

CREATE INDEX IF NOT EXISTS ix_colaboracion_heads_fecha_cierre 
ON public.colaboracion_heads (fecha_cierre);

-- Step 3: Verify that categoria_ingreso with ID 1 exists (fixed category for collaborations)
-- If not, create it
INSERT INTO public.categorias_ingreso (id, nombre, descripcion, activa, fecha_creacion)
SELECT 1, 'Colaboraciones', 'Ingresos por colaboraciones de miembros', true, CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM public.categorias_ingreso WHERE id = 1);

-- Step 4: Add comments for documentation
COMMENT ON COLUMN public.colaboracion_heads.es_cerrado IS 'Indica si la jornada está cerrada (bloqueada para nuevas colaboraciones)';
COMMENT ON COLUMN public.colaboracion_heads.fecha_cierre IS 'Fecha y hora en que se realizó el cierre de la jornada';
COMMENT ON COLUMN public.colaboracion_heads.cerrado_por IS 'Usuario que realizó el cierre de la jornada';

-- Step 5: Optional: Update existing colaboracion_heads to mark old ones as closed
-- Uncomment and adjust the date threshold if you want to automatically close old heads
/*
UPDATE public.colaboracion_heads 
SET 
    es_cerrado = true,
    fecha_cierre = CURRENT_TIMESTAMP,
    cerrado_por = 'Sistema (Migración)'
WHERE fecha < CURRENT_DATE - INTERVAL '7 days'
  AND es_cerrado = false
  AND total > 0;
*/

-- Step 6: Verification query
SELECT 
    'Migration completed successfully' as message,
    (SELECT COUNT(*) FROM public.colaboracion_heads WHERE es_cerrado = true) as jornadas_cerradas,
    (SELECT COUNT(*) FROM public.colaboracion_heads WHERE es_cerrado = false) as jornadas_abiertas,
    (SELECT COUNT(*) FROM public.categorias_ingreso WHERE id = 1) as categoria_colaboraciones_existe;