namespace RS_system.Helpers;

public class UtilidadesStatic
{
    private static string Unidades(long n) => 
        new[] {"", "UNO", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE"}[(int)n];

    private static string Dieces(long n) => 
        new[] {"DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISEIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE"}[(int)n - 10];

    private static string Decenas(long n) => 
        new[] {"", "", "", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA"}[(int)n / 10];
    public static string ConvertirNumeroALetras(decimal numero)
    {
        long entero = (long)Math.Truncate(numero);
        int decimales = (int)((Math.Abs(numero) - Math.Abs(entero)) * 100);
        string resultado = ConvertirEntero(entero);
        return $"{resultado} CON {decimales:00}/100".ToUpper();
    }

    private static string ConvertirEntero(long n)
    {
        if (n == 0) return "CERO";
        if (n == 100) return "CIEN";
        if (n < 0) return "MENOS " + ConvertirEntero(Math.Abs(n));

        string nombre = "";

        if ((n / 1000000) > 0)
        {
            nombre += n / 1000000 == 1 ? "UN MILLON " : ConvertirEntero(n / 1000000) + " MILLONES ";
            n %= 1000000;
        }

        if ((n / 1000) > 0)
        {
            nombre += n / 1000 == 1 ? "MIL " : ConvertirEntero(n / 1000) + " MIL ";
            n %= 1000;
        }

        if ((n / 100) > 0)
        {
            string[] centenas = {"", "CIENTO ", "DOSCIENTOS ", "TRESCIENTOS ", "CUATROCIENTOS ", "QUINIENTOS ", "SEISCIENTOS ", "SETECIENTOS ", "OCHOCIENTOS ", "NOVECIENTOS "};
            nombre += centenas[n / 100];
            n %= 100;
        }

        if (n > 0)
        {
            if (n < 10) nombre += Unidades(n);
            else if (n < 20) nombre += Dieces(n);
            else if (n < 30) nombre += n == 20 ? "VEINTE" : "VEINTI" + Unidades(n % 10);
            else
            {
                nombre += Decenas(n);
                if (n % 10 > 0) nombre += " Y " + Unidades(n % 10);
            }
        }

        return nombre.Trim();
    }
}
