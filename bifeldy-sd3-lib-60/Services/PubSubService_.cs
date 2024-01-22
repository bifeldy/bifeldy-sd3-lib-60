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

using System.Dynamic;

using System.Reactive.Subjects;

namespace bifeldy_sd3_lib_60.Services {

    public interface IPubSubService {
        bool IsExist(string key);
        BehaviorSubject<T> CreateNewBehaviorSubject<T>(T initialValue);
        BehaviorSubject<T> GetGlobalAppBehaviorSubject<T>(string key);
        BehaviorSubject<T> CreateGlobalAppBehaviorSubject<T>(string key, T initialValue);
        void DisposeAndRemoveSubscriber(string key);
    }

    public sealed class CPubSubService : IPubSubService {

        private readonly IConverterService _converter;

        IDictionary<string, dynamic> keyValuePairs = new ExpandoObject();

        public CPubSubService(IConverterService converter) {
            _converter = converter;
        }

        public bool IsExist(string key) {
            return keyValuePairs.ContainsKey(key);
        }

        public BehaviorSubject<T> CreateNewBehaviorSubject<T>(T initialValue) {
            return new BehaviorSubject<T>(initialValue);
        }

        public BehaviorSubject<T> GetGlobalAppBehaviorSubject<T>(string key) {
            if (string.IsNullOrEmpty(key)) {
                throw new Exception("Nama Key Wajib Diisi");
            }
            if (!keyValuePairs.ContainsKey(key)) {
                T defaultValue = _converter.GetDefaultValueT<T>();
                return CreateGlobalAppBehaviorSubject(key, defaultValue);
            }
            return keyValuePairs[key];
        }

        public BehaviorSubject<T> CreateGlobalAppBehaviorSubject<T>(string key, T initialValue) {
            if (string.IsNullOrEmpty(key)) {
                throw new Exception("Nama Key Wajib Diisi");
            }
            if (!keyValuePairs.ContainsKey(key)) {
                keyValuePairs.Add(key, CreateNewBehaviorSubject(initialValue));
            }
            return keyValuePairs[key];
        }

        public void DisposeAndRemoveSubscriber(string key) {
            if (keyValuePairs.ContainsKey(key)) {
                keyValuePairs[key].Dispose();
                keyValuePairs.Remove(key);
            }
        }

    }

}
