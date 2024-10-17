using Soneta.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Soneta.Kadry;
using Soneta.KadryPlace;
using Soneta.Types;
using Rekrutacja.Workers.Template;
using System.Reflection;
using static Soneta.Ksiega.ZestawienieKS;

//Rejetracja Workera - Pierwszy TypeOf określa jakiego typu ma być wyświetlany Worker, Drugi parametr wskazuje na jakim Typie obiektów będzie wyświetlany Worker
[assembly: Worker(typeof(TemplateWorker), typeof(Pracownicy))]
namespace Rekrutacja.Workers.Template
{
    public class TemplateWorker
    {
        //Aby parametry działały prawidłowo dziedziczymy po klasie ContextBase
        public class TemplateWorkerParametry : ContextBase
        {
            [Caption("A")]
            public string Value1 { get; set; }

            [Caption("B")]
            public string Value2 { get; set; }

            [Caption("Data obliczeń")]
            public Date DataObliczen { get; set; }

            [Caption("Figura")]
            public Figure Figure { get; set; }

            public TemplateWorkerParametry(Context context) : base(context)
            {
                this.DataObliczen = Date.Today;
            }
        }
        //Obiekt Context jest to pudełko które przechowuje Typy danych, aktualnie załadowane w aplikacji
        //Atrybut Context pobiera z "Contextu" obiekty które aktualnie widzimy na ekranie
        [Context]
        public Context Cx { get; set; }
        //Pobieramy z Contextu parametry, jeżeli nie ma w Context Parametrów mechanizm sam utworzy nowy obiekt oraz wyświetli jego formatkę
        [Context]
        public TemplateWorkerParametry Parametry { get; set; }
        //Atrybut Action - Wywołuje nam metodę która znajduje się poniżej
        [Action("Kalkulator",
           Description = "Prosty kalkulator ",
           Priority = 10,
           Mode = ActionMode.ReadOnlySession,
           Icon = ActionIcon.Accept,
           Target = ActionTarget.ToolbarWithText)]
        public void WykonajAkcje()
        {
            //Włączenie Debug, aby działał należy wygenerować DLL w trybie DEBUG
            DebuggerSession.MarkLineAsBreakPoint();
            //Pobieranie danych z Contextu
            Pracownik[] employees = null;

			if (this.Cx.Contains(typeof(Pracownik[])))
            {
				employees = Cx[typeof(Pracownik[])] as Pracownik[];

				if (employees == null || employees.Length == 0)
					throw new NullReferenceException();
			}

			//Modyfikacja danych
			//Aby modyfikować dane musimy mieć otwartą sesję, któa nie jest read only
			using (Session nowaSesja = this.Cx.Login.CreateSession(false, false, "ModyfikacjaPracownika"))
            {
                //Otwieramy Transaction aby można było edytować obiekt z sesji
                using (ITransaction trans = nowaSesja.Logout(true))
                {
	                foreach (var emp in employees)
	                {
		                UpdateEmployee(nowaSesja, emp);
					}

                    //Zatwierdzamy zmiany wykonane w sesji
                    trans.CommitUI();
                }
                //Zapisujemy zmiany
                nowaSesja.Save();
            }
        }

        private void UpdateEmployee(Session sesja, Pracownik emp)
        {
	        //Pobieramy obiekt z Nowo utworzonej sesji
	        var pracownik = sesja.Get(emp);

	        //Features - są to pola rozszerzające obiekty w bazie danych, dzięki czemu nie jestesmy ogarniczeni to kolumn jakie zostały utworzone przez producenta
	        pracownik.Features["DataObliczen"] = this.Parametry.DataObliczen;

	        pracownik.Features["Wynik"] = Math.Round(Calculate());
		}

        private double Calculate()
        {
	        double val1 = Parser.StringToIntegralDouble(Parametry.Value1);
	        double val2 = Parser.StringToIntegralDouble(Parametry.Value2);

			switch (Parametry.Figure)
			{
				case Figure.Kwadrat:
					return Math.Pow(val1, 2);
				case Figure.Prostokat:
					return val1 * val2;
				case Figure.Trojkat:
					return val1 * val2 / 2;
				case Figure.Kolo:
					return Math.PI * Math.Pow(val1, 2);
                default:
					throw new Exception("Błąd z typem figury");
			}
		}
    }

    internal static class Parser
    {
	    public static double StringToIntegralDouble(string str)
	    {
		    if (String.IsNullOrEmpty(str))
		    {
			    throw new FormatException("Podany string jest pusty lub nullowy. Niepoprawny format: " + str);
		    }

		    bool isNegative = false;
		    int startIndex = 0;

		    if (str[0] == '-')
		    {
			    isNegative = true;
			    startIndex = 1;
		    }

		    int result = 0;

			for (int i = startIndex; i < str.Length; i++)
		    {
			    char c = str[i];

			    if (!char.IsDigit(c))
			    {
				    throw new FormatException($"Wykryto niepoprawny znak: '{c}'");
			    }

			    result = result * 10 + (int)char.GetNumericValue(c);
		    }

		    return isNegative ? -result : result;
		}
	}
}