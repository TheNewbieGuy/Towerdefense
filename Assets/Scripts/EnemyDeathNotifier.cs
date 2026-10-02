using System;
using UnityEngine;

namespace TowerDefense.Waves
{
    public class EnemyDeathNotifier : MonoBehaviour
    {
        public event Action OnEnemyDied;
        private bool _hasFired = false;
        private bool _isQuitting = false;

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        public void NotifyDied()
        {
            if (_hasFired || _isQuitting) return;
            _hasFired = true;

            OnEnemyDied?.Invoke();
            OnEnemyDied = null;
        }

        private void OnDestroy()
        {
            NotifyDied();
        }
    }
}