using System;
using UnityEngine;

public class DeskBudget : MonoBehaviour
{
    [HideInInspector] public int totalMoney = 3000;
    private BudgetLedger ledger;
    public int Remaining => Ledger.Remaining;
    private BudgetLedger Ledger => ledger ?? (ledger = new BudgetLedger(totalMoney));
    public event Action Changed;
    public void ResetForNewSession()
    {
        ledger = new BudgetLedger(totalMoney);
        Changed?.Invoke();
    }
    public bool TryCommit(PaperDocument paper, int amount)
    {
        if (!Ledger.TryCommit(paper, amount)) return false;
        Changed?.Invoke();
        return true;
    }
}
