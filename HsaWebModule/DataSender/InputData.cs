using System;
using System.Collections.Generic;

namespace HsaWebModule
{
    public class InputData
    {
        public inputDataList inputData;
        public class inputDataList 
        {
            public List<string> strings = new List<string>();
            public List<string> Clone() 
            {
                var data = (inputDataList)this.MemberwiseClone();
                data.strings = new List<string>();
                return data.strings;
            }
        }
        public InputData() 
        {
            inputData = new inputDataList();
        }

        public void AddData(string str)
        {
            try
            {
                if (inputData.strings.Count == 0)
                {
                    inputData.strings = new List<string>();
                    inputData.strings.Add(str);
                }
                else
                {
                    inputData.strings.Add(str);
                }
            }
            catch (Exception ex)
            {
                Program.log.Debug(ex.Message);
            }
        }
    }
}
