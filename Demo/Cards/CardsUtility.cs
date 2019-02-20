
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Demo.Cards
{
    public static class CardsUtility
    {
        public static async Task<string> GetCardText(string cardName)
        {
           
            try
            {
                using (StreamReader reader = File.OpenText($"./Cards/{cardName}.json"))
                {
                    string fileContent = await reader.ReadToEndAsync();
                    if (fileContent != null && fileContent != "")
                    {
                        return fileContent;
                    }
                }
            }
            catch (Exception ex)
            {
                //Log
                throw ex;
            }
            return null;

        }
    }
}
