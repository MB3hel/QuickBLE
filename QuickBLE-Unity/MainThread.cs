using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuickBLE.Unity {

    public class MainThread : MonoBehaviour {
        private static readonly Queue<Action> _executionQueue = new Queue<Action>();

        public void Update() {
            lock (_executionQueue) {
                while (_executionQueue.Count > 0) {
                    _executionQueue.Dequeue().Invoke();
                }
            }
        }

        public void Run(IEnumerator action) {
            lock (_executionQueue) {
                _executionQueue.Enqueue(() => {
                    StartCoroutine(action);
                });
            }
        }
        public void Run(Action action) {
            Run(ActionWrapper(action));
        }


        private static MainThread _instance = null;

        public static bool Exists() {
            return _instance != null;
        }

        public static MainThread Instance() {
            if (!Exists()) {
                throw new Exception("MainThread could not find object (MainThread script) in scene.");
            }
            return _instance;
        }


        void Awake() {
            if (_instance == null) {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        void OnDestroy() {
            _instance = null;
        }

        IEnumerator ActionWrapper(Action action) {
            action();
            yield return null;
        }

    }
}