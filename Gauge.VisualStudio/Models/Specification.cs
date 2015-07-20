using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using main;
using Debugger = System.Diagnostics.Debugger;

namespace Gauge.VisualStudio.Models
{
    public class Specification
    {
        private readonly EnvDTE.Project _project;

        public Specification() : this(ActiveProject)
        {
        }

        public Specification(EnvDTE.Project project)
        {
            _project = project;
        }

        private readonly string _specName;
        private readonly string _fileName;
        private IList<string> _tagsList;

        public Specification(IList<string> tagsList, string specName, string fileName)
        {
            _tagsList = tagsList;
            _specName = specName;
            _fileName = fileName;
        }

        private static EnvDTE.Project ActiveProject
        {
            get { return GaugeDTEProvider.DTE.ActiveDocument.ProjectItem.ContainingProject; }
        }

        public static List<ProtoSpec> GetAllSpecsFromGauge()
        {
            var specifications = new List<ProtoSpec>();
            foreach (var apiConnection in GaugeDTEProvider.GetAllApiConnections())
            {
                var specsRequest = GetAllSpecsRequest.DefaultInstance;
                var apiMessage = APIMessage.CreateBuilder()
                    .SetMessageId(Step.GenerateMessageId())
                    .SetMessageType(APIMessage.Types.APIMessageType.GetAllSpecsRequest)
                    .SetAllSpecsRequest(specsRequest)
                    .Build();

                var bytes = apiConnection.WriteAndReadApiMessage(apiMessage);
                specifications.AddRange(bytes.AllSpecsResponse.SpecsList);
            }

            return specifications;
        }

    }
}