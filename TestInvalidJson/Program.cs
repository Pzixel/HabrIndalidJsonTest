using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TestInvalidJson
{
    class Program
    {
        static void Main(string[] args)
        {
            var filename = args.First(x => !x.Contains("TestInvalidJson"));
            Console.WriteLine("Running with {0}", filename);

            var res = GetResult(filename);

            foreach (var (d, di) in res.DebtorsByPhone.Values.Distinct().Select((x, i) => (x, i)))
            {
                Console.WriteLine("-------------------------------");
                Console.WriteLine($"#{di}: debt: {d.Debt}");
                Console.WriteLine($"companies: [{string.Join(", ", d.Companies)}]\nphones: [{string.Join(", ", d.Phones)}]");
            }
        }

        private static Debtors GetResult(string filename)
        {
            var debtors = new Debtors
            {
                DebtorsByPhone = new Dictionary<string, Debtor>()
            };
            using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 50000))
            using (var sr = new StreamReader(fs))
            using (var reader = new JsonTextReader(sr))
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        JObject obj = JObject.Load(reader);
                        ProcessObject(obj, debtors);
                    }
                }
            }

            return debtors;
        }

        private static void ProcessObject(JObject jObject, Debtors res)
        {
            var dr = ExtractData(jObject);

            var debtorPhone = dr.Phones.FirstOrDefault(p => res.DebtorsByPhone.ContainsKey(p));
            var d = debtorPhone != null
                        ? res.DebtorsByPhone[debtorPhone]
                        : new Debtor
                        {
                            Phones = new HashSet<string>(),
                            Companies = new HashSet<string>()
                        };

            d.Companies.Add(dr.Company);
            foreach (var p in dr.Phones)
            {
                d.Phones.Add(p);
                res.DebtorsByPhone[p] = d;
            }

            d.Debt += dr.Debt;
        }

        private static DebtRec ExtractData(JObject jObject)
        {
            var company = jObject["company"];
            string companyName = (company.Type == JTokenType.Object ? company["name"] : company).Value<string>();

            var phones = jObject["phones"];
            var phonesList = phones == null
                                 ? new List<string>()
                                 : phones.Type == JTokenType.Array
                                     ? phones.Values<object>().Select(x => x.ToString()).ToList()
                                     : new List<string> { phones.Value<object>().ToString() };

            var phone = jObject["phone"];
            if (phone != null)
            {
                phonesList.Add(phone.Value<object>().ToString());
            }

            var debt = jObject["debt"];

            var debtValue = debt.Type == JTokenType.Float
                                ? debt.ToObject<double>()
                                : double.Parse(debt.ToObject<string>());

            return new DebtRec
            {
                Company = companyName,
                Phones = phonesList,
                Debt = debtValue
            };
        }
    }

    class Debtors
    {
        public Dictionary<string, Debtor> DebtorsByPhone { get; set; }
    }

    class Debtor
    {
        public HashSet<string> Companies { get; set; }
        public HashSet<string> Phones { get; set; }
        public double Debt { get; set; }
    }

    struct DebtRec
    {
        public string Company { get; set; }
        public List<string> Phones { get; set; }
        public double Debt { get; set; }
    }
}
