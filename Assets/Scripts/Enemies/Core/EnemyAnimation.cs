using UnityEngine;
using WAD64.Enemies;

public class EnemyAnimation : MonoBehaviour
{
    private Animator animator;
    private AngleToPlayer angleToPlayer;
    private EnemyController enemyController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        angleToPlayer = GetComponent<AngleToPlayer>();
        enemyController = GetComponent<EnemyController>();

        // Подписываемся на событие смерти
        if (enemyController != null)
        {
            enemyController.OnEnemyDied += PlayDeathAnimation;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Не обновляем spriteRotation если враг мертв (чтобы не конфликтовать с анимацией смерти)
        if (animator != null && angleToPlayer != null && enemyController != null && !enemyController.IsDead)
        {
            animator.SetFloat("spriteRotation", angleToPlayer.lastIndex);
        }
    }

    /// <summary>
    /// Проигрывает анимацию смерти, устанавливая trigger параметр Death в Animator
    /// </summary>
    private void PlayDeathAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }
    }

    private void OnDestroy()
    {
        // Отписываемся от события
        if (enemyController != null)
        {
            enemyController.OnEnemyDied -= PlayDeathAnimation;
        }
    }
}
