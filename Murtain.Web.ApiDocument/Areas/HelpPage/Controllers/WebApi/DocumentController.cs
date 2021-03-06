﻿using Murtain.SDK.Models;
using Murtain.SDK.Attributes;
using Murtain.Web.ApiDocument.Areas.HelpPage.ModelDescriptions;
using Murtain.Web.ApiDocument.Areas.HelpPage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.Dispatcher;

namespace Murtain.Web.ApiDocument.Areas.HelpPage.Controllers.WebApi
{
    /// <summary>
    /// 文档管理|提供文档查询服务
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = true)]
    public class DocumentController : ApiController
    {

        private readonly IDocumentationProvider documentationProvider;

        /// <summary>
        /// 构造函数
        /// </summary>
        public DocumentController()
            : this(GlobalConfiguration.Configuration)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="config">System.Web.Http.HttpServer 实例的配置</param>
        public DocumentController(HttpConfiguration config)
        {
            base.Configuration = config;
            this.documentationProvider = Configuration.Services.GetDocumentationProvider();
        }

        /// <summary>
        /// 文档查询
        /// </summary>
        /// <returns></returns>
        [Route("api/documents")]
        public IEnumerable<DocumentModel> Get()
        {
            var documents = new List<DocumentModel>();
            var apiGroups = Configuration.Services.GetApiExplorer()
                                                            .ApiDescriptions
                                                            .ToLookup(api => api.ActionDescriptor.ControllerDescriptor)
                                                            ;
            foreach (var group in apiGroups)
            {
                documents.Add(GetDocument(group));
            }

            return documents;
        }
        /// <summary>
        /// 文档查询
        /// </summary>
        /// <param name="controllerName"></param>
        /// <returns></returns>
        [Route("api/documents/{controller_name}")]
        public DocumentModel Get(string controller_name)
        {
            var group = Configuration.Services.GetApiExplorer()
                                                         .ApiDescriptions
                                                         .ToLookup(api => api.ActionDescriptor.ControllerDescriptor)
                                                         .FirstOrDefault(x => (x.Key.ControllerName.Contains(DefaultHttpControllerSelector.ControllerSuffix) ? x.Key.ControllerName.Remove(x.Key.ControllerName.Length - DefaultHttpControllerSelector.ControllerSuffix.Length) : x.Key.ControllerName).Equals(controller_name, StringComparison.OrdinalIgnoreCase));
            if (group == null)
            {
                throw new Exception($"controller {controller_name} not found.");
            }

            return GetDocument(group);

        }
        /// <summary>
        /// 文档查询
        /// </summary>
        /// <returns></returns>
        [Route("api/documents/{controller_name}/api-descriptions")]
        public IEnumerable<ApiDescriptionModel> GetApiDescriptions(string controller_name)
        {

            var groups = Configuration.Services.GetApiExplorer()
                                                         .ApiDescriptions
                                                         .ToLookup(api => api.ActionDescriptor.ControllerDescriptor)
                                                         .Where(x => (x.Key.ControllerName.Contains(DefaultHttpControllerSelector.ControllerSuffix) ? x.Key.ControllerName.Remove(x.Key.ControllerName.Length - DefaultHttpControllerSelector.ControllerSuffix.Length) : x.Key.ControllerName).Equals(controller_name, StringComparison.OrdinalIgnoreCase));
            if (groups == null)
            {
                throw new Exception($"controller {controller_name} not found.");
            }

            return GetDocumentApiDescriptions(groups);

        }
        /// <summary>
        /// 文档查询
        /// </summary>
        /// <param name="controller_name"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("api/documents/{controller_name}/api-description/{id}")]
        public HelpPageApiModel GetHelpApiModel(string controller_name, string id)
        {
            HelpPageApiModel apiModel = Configuration.GetHelpPageApiModel(id);

            if (apiModel == null)
            {
                throw new Exception($"ApiDescription id {id} not found.");
            }

            ModelDescription modelDescription = null;

            var returnCodeAttribute = ((System.Web.Http.Controllers.ReflectedHttpActionDescriptor)apiModel.ApiDescription.ActionDescriptor).MethodInfo.GetCustomAttributes(false).FirstOrDefault(x => x is ReturnCodeAttribute) as ReturnCodeAttribute;
            if (returnCodeAttribute != null)
            {
                ModelDescriptionGenerator modelDescriptionGenerator = Configuration.GetModelDescriptionGenerator();
                modelDescription = modelDescriptionGenerator.GetOrCreateModelDescription(returnCodeAttribute.ReturnCode);
            }

            DocumentModel document = this.Get(controller_name);

            var returnModel = new HelpPageApiModel
            {
                Id = id,

                ControllerName = document.Description,
                ActionName = document.ApiDescriptions.FirstOrDefault(x => x.Id == id)?.Description,

                HttpMethod = apiModel.ApiDescription.HttpMethod.Method,
                Description = apiModel.ApiDescription.Documentation,
                RelativePath = apiModel.ApiDescription.RelativePath.ToLower(),

                RequestDocumentation = apiModel.RequestDocumentation,
                RequestModelDescription = apiModel.RequestModelDescription,
                ResourceDescription = apiModel.ResourceDescription,

                ReturnCodeModelDescription = modelDescription

            };

            foreach (var item in apiModel.UriParameters)
            {
                returnModel.UriParameters.Add(item);
            }

            var jsonSampleAttribute = ((System.Web.Http.Controllers.ReflectedHttpActionDescriptor)apiModel.ApiDescription.ActionDescriptor).MethodInfo.GetCustomAttributes(false).FirstOrDefault(x => x is JsonSampleAttribute) as JsonSampleAttribute;
            if (jsonSampleAttribute != null)
            {
                IJsonSampleModel sample = Activator.CreateInstance(jsonSampleAttribute.SampleType) as IJsonSampleModel;

                var mediaType = new MediaTypeHeaderValue("application/json");

                var request = sample.GetRequestSampleModel();
                if (request != null)
                {
                    if (!returnModel.SampleRequests.Keys.Any(x => x.Equals(mediaType)))
                    {
                        returnModel.SampleRequests.Add(mediaType, request);
                    }
                }

                var response = sample.GetResponseSampleModel();
                if (response != null)
                {
                    if (!returnModel.SampleResponses.Keys.Any(x => x.Equals(mediaType)))
                    {
                        returnModel.SampleResponses.Add(mediaType, response);
                    }
                }

                var error = sample.GetErrorSampleModel();
                if (error != null)
                {
                    var errorMediaType = new MediaTypeHeaderValue("application/x-error");
                    if (!returnModel.SampleResponses.Keys.Any(x => x.Equals(errorMediaType)))
                    {
                        returnModel.SampleResponses.Add(errorMediaType, error);
                    }
                }
            }

            return returnModel;

        }
        /// <summary>
        /// 文档对象
        /// </summary>
        /// <param name="modelName"></param>
        /// <returns></returns>
        [Route("api/documents/model-description/{modelName}")]
        public ModelDescription GetModelDescription(string modelName)
        {

            ModelDescriptionGenerator modelDescriptionGenerator = Configuration.GetModelDescriptionGenerator();
            ModelDescription modelDescription;
            if (modelDescriptionGenerator.GeneratedModels.TryGetValue(modelName, out modelDescription))
            {
                return modelDescription;
            }
            return null;
        }
        private DocumentModel GetDocument(IGrouping<System.Web.Http.Controllers.HttpControllerDescriptor, System.Web.Http.Description.ApiDescription> group)
        {

            var controllerName = group.Key.ControllerName.IndexOf(DefaultHttpControllerSelector.ControllerSuffix) > 0
                                    ? group.Key.ControllerName.Remove(group.Key.ControllerName.Length - DefaultHttpControllerSelector.ControllerSuffix.Length)
                                    : group.Key.ControllerName;
            ;

            var document = new DocumentModel();

            document.Description = documentationProvider?.GetDocumentation(group.Key);
            if (string.IsNullOrEmpty(document.Description))
            {
                document.Description = controllerName;
            }

            document.ControllerName = controllerName;
            document.ApiDescriptions = GetApiDescriptions(document.ControllerName);

            var segments = group.Key.ControllerType.Namespace.Split(Type.Delimiter);
            document.Namespace = segments[segments.Length - 1] == "Controllers" ? null : segments[segments.Length - 1];

            return document;
        }

        private IList<ApiDescriptionModel> GetDocumentApiDescriptions(IEnumerable<IGrouping<System.Web.Http.Controllers.HttpControllerDescriptor, System.Web.Http.Description.ApiDescription>> groups)
        {

            List<ApiDescriptionModel> models = new List<ApiDescriptionModel>();
            var keys = groups.Select(x => x.Key);
            var versions = keys.Select(x => x.ControllerType.Namespace.Split(Type.Delimiter)[x.ControllerType.Namespace.Split(Type.Delimiter).Length - 1] == "Controllers"
                                            ? null
                                            : "api/" + x.ControllerType.Namespace.Split(Type.Delimiter)[(x.ControllerType.Namespace.Split(Type.Delimiter)).Length - 1] + "/")
                               .Where(x => x != null);

            foreach (var group in groups)
            {
                var segments = group.Key.ControllerType.Namespace.Split(Type.Delimiter);
                var version = segments[segments.Length - 1] == "Controllers" ? null : segments[segments.Length - 1];

                var apis = group.Where(x => version == null ? !versions.Contains(x.Route.RouteTemplate) : x.Route.RouteTemplate.Contains("api/" + version + "/"))
                                .Where(x => !x.Route.RouteTemplate.Contains("api/{namespace}/"));

                foreach (var api in apis)
                {
                    if (!models.Any(x => x.Id == api.GetFriendlyId()))
                    {
                        models.Add(new ApiDescriptionModel
                        {
                            Id = api.GetFriendlyId(),
                            HttpMethod = api.HttpMethod.Method,
                            Namespace = version,
                            Description = !string.IsNullOrEmpty(api.Documentation) ? api.Documentation : api.ActionDescriptor.ActionName,
                            RelativePath = api.RelativePath?.ToLower()
                        });

                    }
                }
            }

            return models;
        }
    }
}
