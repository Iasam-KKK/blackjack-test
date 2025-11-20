<!-- 04af8a3a-9d14-4fc5-b5c8-2530a2402e4e cfc44626-dcb4-480c-b17d-e9073f22e736 -->
# Betting System 2.0 Implementation

## Objective

Replace the current money-based betting system with a health-based betting system where players bet their "soul" (health percentage). Balance = Player Health from GameProgressionManager.

## Current System Analysis

**Old System (Deck.cs)**:

- Uses `_balance` (uint) stored in PlayerPrefs
- Uses `_bet` (uint) for current bet
- `raiseBetButton`, `lowerBetButton`, `placeBetButton`
- Balance displayed in Text component
- Deducts from _balance on bet placement
- Wins/losses modify _balance directly

## New System Design

### 1. Create BettingManager Script

**File**: `Assets/Scripts/Betting/BettingManager.cs`

**Key Features**:

- Singleton pattern
- References GameProgressionManager for balance (playerHealthPercentage)
- Integrates with HealthBarManager for visual updates
- Handles InputField (TMP) for manual entry
- Quick bet buttons (5, 10, 25, 50, 100)
- Validates bets against current health
- Updates Deck.cs with bet amount when placed

**UI References (assign in Inspector)**:

- `betInputField` - TMP_InputField for manual entry
- `betButton5`, `betButton10`, `betButton25`, `betButton50`, `betButton100` - Quick bet buttons
- `placeBetButton` - Confirm bet
- `balanceText` - Display current balance (health %)
- `currentBetText` - Display current bet amount

**Core Methods**:

- `UpdateBalanceDisplay()` - Shows GameProgressionManager.playerHealthPercentage
- `SetBetAmount(float amount)` - Set bet from quick buttons
- `OnInputFieldChanged(string value)` - Handle manual input
- `PlaceBet()` - Validate and place bet (deduct from health, notify Deck)
- `GetCurrentBalance()` - Returns playerHealthPercentage
- `ValidateBet(float amount)` - Check if bet is valid (> 0, <= health)

### 2. Refactor Deck.cs

**Remove**:

- `_balance` variable and Balance property
- `_bet` storage (BettingManager handles this)
- `raiseBetButton`, `lowerBetButton` references
- `RaiseBet()`, `LowerBet()`, `RaiseBetWithMultiplier()`, `LowerBetWithMultiplier()` methods
- Old `PlaceBet()` method
- PlayerPrefs balance save/load
- UpdateBalanceDisplay() method
- Balance initialization logic

**Keep/Modify**:

- `_isBetPlaced` flag
- `placeBetButton` reference (wired by BettingManager)
- Win/loss logic BUT change to modify GameProgressionManager.playerHealthPercentage instead of _balance
- Need public method `StartBettingRound(float betAmount)` called by BettingManager

**Add**:

- Reference to BettingManager
- Public property `CurrentBetAmount` (float) - set by BettingManager when bet placed
- Modify win/loss to use GameProgressionManager health methods

### 3. Win/Loss Integration

**On Player Win**:

- Calculate winnings as float (bet * multiplier)
- Call `GameProgressionManager.Instance.HealPlayer(winnings)`
- Health bar increases visually via HealthBarManager

**On Player Loss**:

- Health already deducted when bet was placed
- Optionally damage player further if needed
- Health bar reflects current health

**On Draw**:

- Call `GameProgressionManager.Instance.HealPlayer(betAmount)` to refund

### 4. UI Flow

**Before Round Starts**:

1. Balance text shows: "Soul: 85%"
2. Player enters bet via buttons or input field
3. Place bet button becomes active when valid bet entered

**On Place Bet**:

1. Deduct from GameProgressionManager.playerHealthPercentage
2. Health bar decreases visually (HealthBarManager auto-updates via events)
3. Disable betting UI
4. Start round in Deck.cs

**After Round**:

1. Win/loss modifies player health directly
2. Health bar updates automatically
3. Return to betting UI for next round

### 5. Betting Constraints

**Min/Max Bets**:

- Min: 1% (configurable)
- Max: playerHealthPercentage (can't bet more than you have)
- Boss-specific limits can modify these

**Validation**:

- Bet must be > 0
- Bet must be <= current health
- If player health < minBet, trigger game over or force minimum

## Implementation Steps

1. Create BettingManager.cs with all UI handling
2. Wire up UI references in Unity Inspector
3. Refactor Deck.cs to remove old betting code
4. Modify win/loss logic to use GameProgressionManager
5. Test betting flow with health bar integration
6. Test win/loss outcomes
7. Test edge cases (low health, max bets, etc.)

### To-dos

- [ ] Create BettingManager.cs singleton with health-based betting
- [ ] Set up InputField and button references in BettingManager
- [ ] Remove old betting code from Deck.cs
- [ ] Modify Deck.cs win/loss to use GameProgressionManager health
- [ ] Test complete betting flow with health integration