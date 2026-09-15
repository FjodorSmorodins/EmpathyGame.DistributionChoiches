int checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
}
var budget = new BudgetLedger(3000);
var babushka = new object();
Check(budget.TryCommit(babushka, 1200) && budget.Remaining == 1800, "First commitment");
Check(!budget.TryCommit(babushka, 1200) && budget.Remaining == 1800, "Duplicate stamp must not charge twice");
var bakers = new object();
Check(!budget.TryCommit(bakers, 1801) && budget.Remaining == 1800, "Overspending must fail");
Check(budget.TryCommit(bakers, 1800) && budget.Remaining == 0, "Rejected spend must not consume the document");
Check(!budget.TryCommit(new object(), 1), "Empty budget");
Check(budget.TryCommit(new object(), 0), "Zero allocation must remain possible");
Check(!budget.TryCommit(new object(), -100), "Negative spending");
Check(!budget.TryCommit(null, 0), "Missing document");
Check(new BudgetLedger(725).Remaining == 725, "Custom starting budget");
Check(new BudgetLedger(-1).Remaining == 0, "Negative budget is clamped");
var gate = new ReturnSubmissionGate();
bool Tick(bool inside, bool held, bool deliberate, bool releasedInside, int version, float dt = 0.3f) =>
    gate.Tick(inside, held, deliberate, releasedInside, version, dt, 0.25f);
gate.Arm(true, 1);
Check(!Tick(true, false, true, true, 1), "Stamping a paper left in its slot is not a return");
Check(Tick(true, false, true, true, 2), "A fresh release after a small lift inside the zone is a valid return");
Check(!Tick(false, true, false, false, 2), "Held outside slot");
Check(!Tick(true, true, false, false, 2), "Still held inside slot");
Check(!Tick(true, false, false, true, 2), "Tracking cancellation is not a return");
Check(!Tick(true, false, true, false, 3), "Release outside followed by sliding into slot is not a deliberate insertion");
Check(!Tick(true, false, true, true, 4, 0.1f), "Require settling time");
Check(Tick(true, false, true, true, 4, 0.16f), "Correct hand release in correct slot");
Check(!Tick(true, true, false, true, 4), "Regrabbing invalidates a ready return");
Check(!Tick(true, false, true, true, 5, 0.1f), "Regrab resets settling time");
Check(!Tick(false, false, true, true, 5), "Wrong lane or moving outside invalidates return");
gate.Arm(false, 5);
Check(Tick(true, false, true, true, 6), "Stamp while holding outside, then return");
gate.Arm(true, 6); // Recovery re-arms using the current release version.
Check(!Tick(true, false, true, true, 6), "Recovery must not submit the paper");
Check(!Tick(false, true, false, false, 6), "Pick recovered paper up and remove it");
Check(Tick(true, false, true, true, 7), "Recovered paper can still be deliberately returned");
gate.Arm(false, 7);
Check(!Tick(false, false, true, true, 8), "A release above the slot must wait for the paper to land");
Check(Tick(true, false, true, true, 8), "Paper released above the slot is accepted after landing");
Console.WriteLine($"Passed {checks} budget and paper-return regression checks.");
