// Copyright [2014, 2015] [ThoughtWorks Inc.](www.thoughtworks.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace Gauge.VisualStudio.Core.Exceptions
{
    [Serializable]
    public abstract class GaugeExceptionBase : Exception
    {
        protected abstract string ErrorCode { get; }

        protected GaugeExceptionBase(string errorMessage) : base(errorMessage)
        {
            Data.Add("ErrorCode", ErrorCode);
        }

        public override string ToString()
        {
            var errorString = base.ToString();
            foreach (var dataKey in Data.Keys)
            {
                errorString += $"{dataKey} : {Data[dataKey]}; ";
            }

            errorString += $"Refer https://info.getgauge.io/{ErrorCode} for more details.";
            return errorString;
        }
    }
}