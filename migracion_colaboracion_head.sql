-- Migration script for ColaboracionHead Master-Detail refactoring
-- This script adds the new colaboracion_heads table and modifies the colaboraciones table

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

-- Step 5: Optional: Migrate existing data (if needed)
-- This would create heads for existing colaboraciones based on their fecha_registro
-- Uncomment and run if you want to migrate existing data

/*
DO $$
DECLARE
    rec RECORD;
    head_id BIGINT;
BEGIN
    -- Group existing colaboraciones by date
    FOR rec IN 
        SELECT DISTINCT DATE(fecha_registro) as fecha_dia
        FROM public.colaboraciones
        WHERE colaboracion_head_id IS NULL
        ORDER BY fecha_dia
    LOOP
        -- Create head for this date
        INSERT INTO public.colaboracion_heads (fecha, total, creado_en, actualizado_en, creado_por)
        VALUES (
            rec.fecha_dia,
            (SELECT COALESCE(SUM(monto_total), 0) FROM public.colaboraciones WHERE DATE(fecha_registro) = rec.fecha_dia),
            CURRENT_TIMESTAMP,
            CURRENT_TIMESTAMP,
            'Sistema (Migración)'
        )
        RETURNING id INTO head_id;
        
        -- Update colaboraciones with the new head_id
        UPDATE public.colaboraciones
        SET colaboracion_head_id = head_id
        WHERE DATE(fecha_registro) = rec.fecha_dia;
        
        RAISE NOTICE 'Created head % for date %', head_id, rec.fecha_dia;
    END LOOP;
END $$;
*/

-- Step 6: Verify the migration
COMMENT ON TABLE public.colaboracion_heads IS 'Encabezados de jornadas de colaboración (esquema Maestro-Detalle)';
COMMENT ON COLUMN public.colaboracion_heads.fecha IS 'Fecha de la jornada (única por día)';
COMMENT ON COLUMN public.colaboracion_heads.total IS 'Total recaudado en la jornada';
COMMENT ON COLUMN public.colaboraciones.colaboracion_head_id IS 'Referencia al encabezado de jornada (Maestro-Detalle)';

-- Step 7: Show summary
SELECT 
    'Migration completed successfully' as message,
    (SELECT COUNT(*) FROM public.colaboracion_heads) as total_heads,
    (SELECT COUNT(*) FROM public.colaboraciones WHERE colaboracion_head_id IS NOT NULL) as colaboraciones_with_head,
    (SELECT COUNT(*) FROM public.colaboraciones WHERE colaboracion_head_id IS NULL) as colaboraciones_without_head;