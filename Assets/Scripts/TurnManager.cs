using UnityEngine;
using System.Collections.Generic;

public class TurnManager : MonoBehaviour
{
    [SerializeField] private List<EnemyMovement> enemies = new List<EnemyMovement>();
    
    public void RegisterEnemy(EnemyMovement enemy)
    {
        if (!enemies.Contains(enemy))
        {
            enemies.Add(enemy);
        }
    }
    
    public event System.Action OnTurnCompleted;

    public void OnPlayerMoved()
    {
        // プレイヤーが移動したら全エネミーを動かす
        foreach (EnemyMovement enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.TakeTurn();
            }
        }
        
        OnTurnCompleted?.Invoke();
    }
    


    public void StunAllEnemies(int turns)
    {
        foreach (EnemyMovement enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.Stun(turns);
            }
        }
    }
}
