using System;
using System.Text;

namespace HsaWebModule
{
    public abstract class CustomQueueBase
    {
        public bool useConsole = false;
        private string delimeter = "0x0a";
        public int defaultSize = 20;
        public int curruntQueueSize = 0;
        public int stringLength = (int)Math.Pow(2, 10);
        public int currentPosition, lastPosiotion = 0;
        public string dequeueString = string.Empty;
        public StringBuilder customQueue = new StringBuilder();
        public abstract void Enqueue(string stringValue);

        public abstract string Dequeue();

        public void InputString(string stringValue)
        {
            if (string.IsNullOrEmpty(stringValue))
            {
                return;
            }
            if (stringValue.Length > stringLength)
            {
                // changeQueueSize and intput String Split over 1024
                if (useConsole) Program.WriteLog("ReSize CustomQueue.");
                SplitStringQueue(stringValue);
            }
            else
            {
                if (lastPosiotion == 0)
                {
                    customQueue.Append(stringValue);
                    lastPosiotion += stringValue.Length;
                    currentPosition += 1;
                }
                else
                {
                    customQueue.Insert(0, $"{stringValue}{delimeter}");
                    lastPosiotion = customQueue.ToString().LastIndexOf(delimeter);
                    currentPosition += 1;
                }
            }
        }
        private void SplitStringQueue(string stringValue)
        {
            try
            {
                int count = (stringValue.Length / stringLength);
                int changeSize = currentPosition + count;
                int addSize = (stringValue.Length % stringLength);
                changeSize += (addSize > 0 ? 1 : 0);

                StringBuilder tempQueue = new StringBuilder(stringLength * changeSize);

                char[] tempStringArray = stringValue.ToCharArray();
                string tempString = string.Empty;

                for (int i = 0; i < tempStringArray.Length; i++)
                {
                    tempString += tempStringArray[i].ToString();
                    if (i > 0 && ((i % stringLength == 0) || (i > (stringLength * count) && i == tempStringArray.Length - 1)))
                    {
                        if (i == stringLength) tempQueue.Append(tempString);
                        else tempQueue.Insert(0, $"{tempString}{delimeter}");
                        tempString = string.Empty;
                    }
                }

                if (!string.IsNullOrEmpty(tempQueue.ToString())) customQueue.Insert(0, tempQueue.ToString());
                lastPosiotion = customQueue.ToString().LastIndexOf(delimeter) - 4;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public string GetString()
        {
            string returnValue = string.Empty;

            try
            {
                int lastSize = customQueue.ToString().Length - lastPosiotion == 0 ? 0 : customQueue.ToString().Length - lastPosiotion;

                if (lastSize == 0)
                {
                    returnValue = customQueue.ToString().Substring(0, lastPosiotion);
                    customQueue.Clear();
                    lastPosiotion = 0;
                    currentPosition = 0;
                }
                else
                {
                    if (lastPosiotion != 0)
                    {
                        returnValue = customQueue.ToString().Substring(lastPosiotion + 4, lastSize - 4).Replace(delimeter, "");
                        customQueue.Remove(lastPosiotion + 4, lastSize - 4);
                        lastPosiotion = customQueue.ToString().LastIndexOf(delimeter) - 4;
                    }
                    else
                    {
                        returnValue = customQueue.ToString().Substring(lastPosiotion, lastSize).Replace(delimeter, "");
                        customQueue.Clear();
                    }
                    if (lastPosiotion < 0) lastPosiotion = 0;
                }
            }
            catch (Exception)
            {
                returnValue = string.Empty;
                throw;
            }
            finally
            {
                dequeueString = returnValue;
            }
            return returnValue;
        }

        public void AutoExtractQueue(Action<string> func)
        {
            while (true)
            {
                if (customQueue.ToString().Length == 0) break;
                func(Dequeue());
            }
        }
        public void AutoExtractQueue()
        {
            if (useConsole) Program.WriteLog("Call AutoExtractQueue.");
            while (true)
            {
                if (customQueue.ToString().Length == 0) break;
                string dequeueStr = Dequeue();
                if (!string.IsNullOrEmpty(dequeueStr) && useConsole) Program.WriteLog($"AutoExtractQueue:Dequeue [{dequeueStr}]");
            }
        }
    }
}
