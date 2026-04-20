-- Simple migration script for ColaboracionHead Master-Detail refactoring
-- This script only creates the new structure without migrating existing data

-- Step 1: Create the new colaboracion_heads table
CREATE TABLE IF NOT EXISTS public.colaboracion_heads (
    id BIGSERIAL PRIMARY KEY,
    fecha DATE NOT NULL,
    total NUMERIC(12,2) NOT NULL DEFAULT 0,
    creado_en TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    actualizado_en TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    creado_por VARCHAR(100)
);

-- Create unique index for one head per date
CREATE UNIQUE INDEX IF NOT EXISTS ix_colaboracion_heads_fecha 
ON public.colaboracion_heads (fecha);

-- Step 2: Add the new column to colaboraciones table
ALTER TABLE public.colaboraciones 
ADD COLUMN IF NOT EXISTS colaboracion_head_id BIGINT;

-- Step 3: Add foreign key constraint
ALTER TABLE public.colaboraciones
ADD CONSTRAINT fk_colaboraciones_colaboracion_head_id 
FOREIGN KEY (colaboracion_head_id) 
REFERENCES public.colaboracion_heads(id) 
ON DELETE RESTRICT;

-- Step 4: Create index for better performance
CREATE INDEX IF NOT EXISTS ix_colaboraciones_colaboracion_head_id 
ON public.colaboraciones (colaboracion_head_id);

-- Step 5: Add comments
COMMENT ON TABLE public.colaboracion_heads IS 'Encabezados de jornadas de colaboración (esquema Maestro-Detalle)';
COMMENT ON COLUMN public.colaboracion_heads.fecha IS 'Fecha de la jornada (única por día)';
COMMENT ON COLUMN public.colaboracion_heads.total IS 'Total recaudado en la jornada';
COMMENT ON COLUMN public.colaboraciones.colaboracion_head_id IS 'Referencia al encabezado de jornada (Maestro-Detalle)';

-- Step 6: Verification query
SELECT 
    'Migration completed. New colaboraciones will use the Master-Detail pattern.' as message,
    'Existing colaboraciones will have NULL colaboracion_head_id' as note,
    'New colaboraciones will automatically create/use ColaboracionHead records' as behavior;