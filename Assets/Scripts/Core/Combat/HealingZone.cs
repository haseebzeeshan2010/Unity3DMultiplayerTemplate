using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
public class HealingZone : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Image healPowerBar;

    [Header("Settings")]

    [SerializeField] private int maxHealPower = 30; // how many times it can restore health
    [SerializeField] private float healCooldown = 5f; // how long it takes to recharge the heal power

    [SerializeField] private float healTickRate = 1f; // how often it heals
    [SerializeField] private int coinsPerTick = 10; // how many coins it consumes per tick
    [SerializeField] private int healthPerTick = 10; // how much health it restores per tick
    
    private float remainingCooldown; // how much time is left until the heal power is recharged
    private float tickTimer; // how much time is left until the next heal tick
    private List<TankPlayer> playersInZone = new List<TankPlayer>();

    private NetworkVariable<int> HealPower = new NetworkVariable<int>(); // how much heal power is left

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            HealPower.OnValueChanged += HandleHeaLPowerChanged; // Subscribe to the heal power changed event
            HandleHeaLPowerChanged(0, HealPower.Value); // Initialize the heal power bar
        }

        if(IsServer)
        {
            HealPower.Value = maxHealPower; // Set the initial heal power value
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            HealPower.OnValueChanged += HandleHeaLPowerChanged; // Subscribe to the heal power changed event
            HandleHeaLPowerChanged(0, HealPower.Value); // Initialize the heal power bar
        }
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!IsServer) return; // Only the server should handle healing

        if(!col.attachedRigidbody.TryGetComponent<TankPlayer>(out TankPlayer player)) {return;} // Check if the collider is a player

        playersInZone.Add(player); // Add the player to the list of players in the zone

    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (!IsServer) return; // Only the server should handle healing
        
        if(!col.attachedRigidbody.TryGetComponent<TankPlayer>(out TankPlayer player)) {return;} // Check if the collider is a player

        playersInZone.Remove(player); // Remove the player to the list of players in the zone
    

    }

    private void Update()
    {
        if (!IsServer) { return; } // Only the server should handle healing

        if (remainingCooldown > 0f)
        {
            remainingCooldown -= Time.deltaTime; // Decrease the cooldown timer

            if (remainingCooldown <= 0f)
            {
                HealPower.Value = maxHealPower; // Reset the heal power to max
            }
            else
            {
                return;
            }
        }

        tickTimer += Time.deltaTime; // Increase the tick timer
        if(tickTimer >= 1 / healTickRate)
        {
            foreach (TankPlayer player in playersInZone) // Loop through all players in the zone
            {
                if (HealPower.Value <= 0) { break; } // Stop healing if there is no heal power left

                if (player.Health.CurrentHealth.Value == player.Health.MaxHealth) { continue; } // Skip if the player is already at max health

                if (player.Wallet.TotalCoins.Value < coinsPerTick) { continue; } // Skip if the player doesn't have enough coins


                player.Wallet.SpendCoins(coinsPerTick); // Deduct the coins from the player's wallet
                player.Health.RestoreHealth(healthPerTick); // Heal the player

                HealPower.Value -= 1; // Decrease the heal power by 1

                if(HealPower.Value == 0) // Check if the heal power is depleted
                {
                    remainingCooldown = healCooldown; // Start the cooldown timer
                    
                }
            }

            tickTimer = tickTimer % (1/healTickRate); // Reset the tick timer
        }
        
    }


    private void HandleHeaLPowerChanged(int oldHealPower, int newHealPower)
    {
        
        healPowerBar.fillAmount = (float)newHealPower / maxHealPower; // Update the fill amount of the heal power bar
        
    }
}
