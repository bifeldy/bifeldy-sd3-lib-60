﻿/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Role Decorator Class & Function Di Controller
 *              :: Tidak Untuk Didaftarkan Ke DI Container
 * 
 */

using System.Net.Mime;

using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace bifeldy_sd3_lib_60.AttributeFilterDecorator {

    public class SwaggerMediaTypesOperationFilter : IOperationFilter {

        public static readonly List<string> AcceptedContentType = new() {
            MediaTypeNames.Application.Json,
            MediaTypeNames.Application.Xml
        };

        public void Apply(OpenApiOperation operation, OperationFilterContext context) {
            if (operation.RequestBody == null) {
                return;
            }

            operation.RequestBody.Content = FilterMediaTypes(operation.RequestBody.Content);
            foreach (KeyValuePair<string, OpenApiResponse> response in operation.Responses) {
                response.Value.Content = FilterMediaTypes(response.Value.Content);
            }
        }

        private static Dictionary<string, OpenApiMediaType> FilterMediaTypes(IDictionary<string, OpenApiMediaType> apiMediaTypes) {
            var _act = new Dictionary<string, OpenApiMediaType>();
            foreach (string act in AcceptedContentType) {
                if (apiMediaTypes.TryGetValue(act, out OpenApiMediaType applicationJson)) {
                    _act.Add(act, applicationJson);
                }
            }

            return _act;
        }

    }

}