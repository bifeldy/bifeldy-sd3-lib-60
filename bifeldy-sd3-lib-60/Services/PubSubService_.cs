/**
 * 
 * Author       :: Basilius Bias Astho Christyono
 * Phone        :: (+62) 889 236 6466
 * 
 * Department   :: IT SD 03
 * Mail         :: bias@indomaret.co.id
 * 
 * Catatan      :: Rx Pub-Sub
 *              :: Harap Didaftarkan Ke DI Container
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace bifeldy_sd3_lib_60.Services {

    public interface IPubSubService {
        BehaviorSubject<T> CreateNewBehaviorSubject<T>(T initialValue);
        BehaviorSubject<T> GetGlobalAppBehaviorSubject<T>(string variableName);
        BehaviorSubject<T> CreateGlobalAppBehaviorSubject<T>(string variableName, T initialValue);
        void Unsubscribe(string variableName);
    }

    public sealed class CPubSubService : IPubSubService {

        private readonly IConverterService _converter;

        IDictionary<string, BehaviorSubject<dynamic>> keyValuePairs = new Dictionary<string, BehaviorSubject<dynamic>>();

        public CPubSubService(IConverterService converter) {
            _converter = converter;
        }

        public BehaviorSubject<T> CreateNewBehaviorSubject<T>(T initialValue) {
            return new BehaviorSubject<T>(initialValue);
        }

        public BehaviorSubject<T> GetGlobalAppBehaviorSubject<T>(string variableName) {
            if (!keyValuePairs.ContainsKey(variableName)) {
                T defaultValue = _converter.GetDefaultValueT<T>();
                return CreateGlobalAppBehaviorSubject(variableName, defaultValue);
            }
            return (dynamic) keyValuePairs[variableName];
        }

        public BehaviorSubject<T> CreateGlobalAppBehaviorSubject<T>(string variableName, T initialValue) {
            if (!keyValuePairs.ContainsKey(variableName)) {
                keyValuePairs.Add(variableName, (dynamic) CreateNewBehaviorSubject(initialValue));
            }
            return (dynamic) keyValuePairs[variableName];
        }

        public void Unsubscribe(string variableName) {
            if (keyValuePairs.ContainsKey(variableName)) {
                keyValuePairs[variableName].Dispose();
                keyValuePairs.Remove(variableName);
            }
        }

    }

}
