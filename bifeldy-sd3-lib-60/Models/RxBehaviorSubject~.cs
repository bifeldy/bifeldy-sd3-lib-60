/**
* 
* Author       :: Basilius Bias Astho Christyono
* Phone        :: (+62) 889 236 6466
* 
* Department   :: IT SD 03
* Mail         :: bias@indomaret.co.id
* 
* Catatan      :: Template Behavior Subject
*              :: Model Supaya Tidak Perlu Install Package Nuget RxNET
* 
*/

using System.Reactive.Subjects;

namespace bifeldy_sd3_lib_60.Models {

    // Wrapper Saja ~ Gak Bisa Inherit Langsung Dari
    // sealed BehaviorSubject

    public sealed class RxBehaviorSubject<T> : SubjectBase<T>, IDisposable {

        BehaviorSubject<T> _backing = null;

        public RxBehaviorSubject(T value) {
            _backing = new BehaviorSubject<T>(value);
        }

        public override bool HasObservers => _backing.HasObservers;

        public override bool IsDisposed => _backing.IsDisposed;

        public T Value => _backing.Value;

        public bool TryGetValue(out T value) {
            return _backing.TryGetValue(out value);
        }

        public override void Dispose() {
            _backing.Dispose();
        }

        public override void OnCompleted() {
            _backing.OnCompleted();
        }

        public override void OnError(Exception error) {
            _backing.OnError(error);
        }

        public override void OnNext(T value) {
            _backing.OnNext(value);
        }

        public override IDisposable Subscribe(IObserver<T> observer) {
            return _backing.Subscribe(observer);
        }

    }

}
