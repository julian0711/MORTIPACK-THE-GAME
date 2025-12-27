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
            Debug.Log($"Enemy registered: {enemy.name}");
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
    
    private void Update()
    {
        // デバッグ用
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"Registered enemies: {enemies.Count}");
        }
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
        Debug.Log($"[TurnManager] Stunned all enemies for {turns} turns.");
    }
}
