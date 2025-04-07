using FlaxEngine;

namespace Game
{
    /// <summary>
    /// Gestionnaire de temps simulant une horloge 24h
    /// </summary>
    public class TimeOTDHandler : Script
    {
        /// <summary>
        /// Multiplicateur de vitesse d'écoulement du temps
        /// </summary>
        public float TimeMultiplier = 32f;
        
        /// <summary>
        /// Intervalle en secondes pour incrémenter le temps simulé
        /// </summary>
        [Range(0.1f, 60f)]
        public float Interval = 5f;
        
        /// <summary>
        /// Temps accumulé en secondes (temps de jeu interne)
        /// </summary>
        private float _totalGameSeconds;
        
        /// <summary>
        /// Dernier temps affiché pour éviter les logs inutiles
        /// </summary>
        private int _lastLoggedTime;
        
        /// <summary>
        /// Propriétés en lecture seule pour accéder aux composants de temps
        /// </summary>
        public int Hours => (int)(_totalGameSeconds / 3600) % 24;
        public int Minutes => (int)(_totalGameSeconds / 60) % 60;
        public int Seconds => (int)_totalGameSeconds % 60;
        
        /// <summary>
        /// Mise à jour appelée à chaque frame
        /// </summary>
        public override void OnUpdate()
        {
            // Calcul du temps écoulé
            _totalGameSeconds += Time.DeltaTime * TimeMultiplier;
            
            // Vérification si l'intervalle est atteint
            int currentTimeAsInt = (int)_totalGameSeconds;
            if (currentTimeAsInt % Interval == 0 && currentTimeAsInt != _lastLoggedTime)
            {
                _lastLoggedTime = currentTimeAsInt;
                
                // Format plus lisible pour le log
                //Debug.Log($"{Hours:D2}h{Minutes:D2}m{Seconds:D2}s");
            }
        }
        
        /// <summary>
        /// Définir le temps à une valeur spécifique
        /// </summary>
        public void SetTime(int hours, int minutes, int seconds)
        {
            _totalGameSeconds = (hours % 24) * 3600 + (minutes % 60) * 60 + (seconds % 60);
        }
        
        /// <summary>
        /// Obtenir le temps actuel sous forme de chaîne formatée
        /// </summary>
        public string GetTimeString(bool includeSeconds = true)
        {
            return includeSeconds 
                ? $"{Hours:D2}:{Minutes:D2}:{Seconds:D2}" 
                : $"{Hours:D2}:{Minutes:D2}";
        }
    }
}