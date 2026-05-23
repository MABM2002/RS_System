-- =============================================================
-- Seed del Catálogo de Cuentas Contables (estructura jerárquica)
-- Cuentas estándar para iglesia local
-- =============================================================

-- 1. ACTIVO (1)
INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('1', 'ACTIVO', NULL, 1, true, NOW());

-- 1.1 Activo Corriente
INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('1.1', 'Activo Corriente', (SELECT id FROM cuentas_contables WHERE codigo = '1'), 1, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('1.1.01', 'Caja General', (SELECT id FROM cuentas_contables WHERE codigo = '1.1'), 1, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('1.1.02', 'Bancos', (SELECT id FROM cuentas_contables WHERE codigo = '1.1'), 1, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('1.1.03', 'Cuentas por Cobrar', (SELECT id FROM cuentas_contables WHERE codigo = '1.1'), 1, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('1.1.04', 'Inventarios', (SELECT id FROM cuentas_contables WHERE codigo = '1.1'), 1, true, NOW());

-- 1.2 Activo No Corriente
INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('1.2', 'Activo No Corriente', (SELECT id FROM cuentas_contables WHERE codigo = '1'), 1, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('1.2.01', 'Propiedades y Equipos', (SELECT id FROM cuentas_contables WHERE codigo = '1.2'), 1, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('1.2.02', 'Depreciación Acumulada', (SELECT id FROM cuentas_contables WHERE codigo = '1.2'), 1, true, NOW());

-- 2. PASIVO (2)
INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('2', 'PASIVO', NULL, 2, true, NOW());

-- 2.1 Pasivo Corriente
INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('2.1', 'Pasivo Corriente', (SELECT id FROM cuentas_contables WHERE codigo = '2'), 2, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('2.1.01', 'Cuentas por Pagar', (SELECT id FROM cuentas_contables WHERE codigo = '2.1'), 2, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('2.1.02', 'Obligaciones Tributarias', (SELECT id FROM cuentas_contables WHERE codigo = '2.1'), 2, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('2.1.03', 'Acreedores Varios', (SELECT id FROM cuentas_contables WHERE codigo = '2.1'), 2, true, NOW());

-- 3. CAPITAL (3)
INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('3', 'CAPITAL', NULL, 3, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('3.1.01', 'Capital Institucional', (SELECT id FROM cuentas_contables WHERE codigo = '3'), 3, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('3.1.02', 'Resultados Acumulados', (SELECT id FROM cuentas_contables WHERE codigo = '3'), 3, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('3.1.03', 'Resultado del Ejercicio', (SELECT id FROM cuentas_contables WHERE codigo = '3'), 3, true, NOW());

-- 4. INGRESO (4)
INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('4', 'INGRESOS', NULL, 4, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('4.1.01', 'Ingresos por Diezmos', (SELECT id FROM cuentas_contables WHERE codigo = '4'), 4, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('4.1.02', 'Ingresos por Ofrendas', (SELECT id FROM cuentas_contables WHERE codigo = '4'), 4, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('4.1.03', 'Ingresos por Colaboraciones', (SELECT id FROM cuentas_contables WHERE codigo = '4'), 4, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('4.1.04', 'Ingresos por Eventos', (SELECT id FROM cuentas_contables WHERE codigo = '4'), 4, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('4.1.99', 'Otros Ingresos', (SELECT id FROM cuentas_contables WHERE codigo = '4'), 4, true, NOW());

-- 5. GASTO (5)
INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('5', 'GASTOS', NULL, 5, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('5.1.01', 'Gastos Operativos', (SELECT id FROM cuentas_contables WHERE codigo = '5'), 5, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('5.1.02', 'Gastos de Personal', (SELECT id FROM cuentas_contables WHERE codigo = '5'), 5, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('5.1.03', 'Servicios Públicos', (SELECT id FROM cuentas_contables WHERE codigo = '5'), 5, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('5.1.04', 'Mantenimiento', (SELECT id FROM cuentas_contables WHERE codigo = '5'), 5, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('5.1.05', 'Gastos de Transporte', (SELECT id FROM cuentas_contables WHERE codigo = '5'), 5, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('5.1.06', 'Materiales y Suministros', (SELECT id FROM cuentas_contables WHERE codigo = '5'), 5, true, NOW());

INSERT INTO cuentas_contables (codigo, nombre, padre_id, tipo, activa, fecha_creacion)
VALUES ('5.1.99', 'Otros Gastos', (SELECT id FROM cuentas_contables WHERE codigo = '5'), 5, true, NOW());

-- =============================================================
-- Configurar Cuenta de Caja por Defecto (1.1.01 - Caja General)
-- =============================================================
INSERT INTO configuraciones (clave, valor, descripcion)
VALUES ('CUENTA_CAJA_DEFAULT_ID', 
        (SELECT id::text FROM cuentas_contables WHERE codigo = '1.1.01'),
        'ID de la cuenta contable de caja/bancos usada como contrapartida por defecto en asientos automáticos')
ON CONFLICT (clave) DO NOTHING;
