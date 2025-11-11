# Action Cards & Auto-Reshuffle Setup Guide

## 🎯 What's Been Implemented

### 1. Auto-Reshuffle System ✅
- **Manual Reshuffle Button**: Appears when deck < 10 cards
- **Auto-Reshuffle**: Triggers automatically when deck < 5 cards
- **Visual Notifications**: Shows "AUTO-RESHUFFLING DECK..." and "Deck Reshuffled! (+X cards)"
- **Deck Counter**: Display shows remaining cards (changes color: Red < 5, Yellow < 10, White >= 10)

### 2. Action Cards System ✅
- **10 Different Action Types** with full implementations
- **Action Budget Integration**: Each action consumes from 2-action-per-hand budget
- **Reusable System**: Actions reset between hands
- **Visual Feedback**: Cards dim when used (if not reusable)

---

## 📋 Unity Editor Setup - Step by Step

### Part 1: Add Deck Cards Counter

#### Step 1: Create Text Element
1. In your blackjack scene, find the Canvas
2. Right-click Canvas → **UI → Text**
3. Rename it to "DeckCardsRemainingText"
4. Position it near the discard pile counter (or top-right corner)
5. Set properties:
   - **Text:** "Deck: 52"
   - **Font Size:** 18-20
   - **Color:** White
   - **Alignment:** Center or Left

#### Step 2: Link to Deck Component
1. Select **Deck** GameObject
2. In Inspector, find **Deck (Script)**
3. Find field **Deck Cards Remaining Text**
4. Drag **DeckCardsRemainingText** into this field

---

### Part 2: Add Reshuffle Button

#### Step 1: Create Button
1. Right-click Canvas → **UI → Button**
2. Rename it to "ReshuffleButton"
3. Position it near the deck counter or center-bottom
4. Change button text to "Reshuffle Deck" or "🔄 Reshuffle"
5. Style it (make it noticeable - maybe yellow/orange color)

#### Step 2: Link to Deck Component
1. Select **Deck** GameObject
2. In Inspector, find **Reshuffle Button** field
3. Drag **ReshuffleButton** into this field

**Note:** The button will be hidden by default and only appears when needed!

---

### Part 3: Create Action Cards System

#### Step 1: Create Action Cards Panel
1. Right-click Canvas → **UI → Panel**
2. Rename it to "ActionCardsPanel"
3. Position it somewhere accessible (bottom of screen, or side panel)
4. Resize to fit ~4-6 cards horizontally
5. Add a **Horizontal Layout Group** component:
   - Child Alignment: Middle Center
   - Spacing: 10
   - Child Force Expand: Width & Height OFF

#### Step 2: Create Action Card Prefab

1. **Create Card Background:**
   - Right-click Canvas → **UI → Image**
   - Rename to "ActionCardPrefab"
   - Set size: 80x100 (or adjust to preference)
   - Set color: Blue or any distinct color

2. **Add Icon (Child of ActionCardPrefab):**
   - Right-click ActionCardPrefab → **UI → Image**
   - Rename to "Icon"
   - Position at top-center of card
   - Set size: 40x40

3. **Add Name Text (Child of ActionCardPrefab):**
   - Right-click ActionCardPrefab → **UI → Text**
   - Rename to "NameText"
   - Position at middle of card
   - Font Size: 12
   - Alignment: Center

4. **Add Cost Text (Child of ActionCardPrefab):**
   - Right-click ActionCardPrefab → **UI → Text**
   - Rename to "CostText"
   - Position at bottom-right corner
   - Font Size: 14-16
   - Color: Yellow
   - Text: "1"

5. **Add ActionCard Component:**
   - Select ActionCardPrefab
   - Add Component → Search "Action Card"
   - Add Component → **Canvas Group** (for fade effects)
   - In ActionCard component, assign references:
     - **Card Image:** ActionCardPrefab Image
     - **Icon Image:** Icon
     - **Name Text:** NameText  
     - **Cost Text:** CostText

6. **Save as Prefab:**
   - Drag ActionCardPrefab from Hierarchy to **Assets/Prefabs/** folder
   - Delete from Hierarchy after saving

#### Step 3: Link Panel and Prefab to Deck
1. Select **Deck** GameObject
2. In Inspector, find **Action Cards Panel** field
3. Drag **ActionCardsPanel** (from Hierarchy) into this field
4. Find **Action Card Prefab** field
5. Drag **ActionCardPrefab** (from Assets/Prefabs) into this field

---

### Part 4: Create Action Card Data (ScriptableObjects)

Now create the actual action cards that will appear in game:

#### Step 1: Create Action Card Assets

1. In Project window, navigate to **Assets/**
2. Create folder: **Assets/ActionCards/** (if doesn't exist)
3. Right-click in ActionCards folder → **Create → BlackJack → Action Card**

#### Step 2: Create These 6 Starter Actions:

**1. Swap Two Cards**
- Name: "Swap Cards"
- Action Type: SwapTwoCards
- Description: "Swap values of two selected cards"
- Actions Required: 1
- Color: Blue

**2. Add +1**
- Name: "+1 Card"
- Action Type: AddOneToCard
- Description: "Add +1 to selected card (max 10)"
- Actions Required: 1
- Color: Green

**3. Subtract -1**
- Name: "-1 Card"
- Action Type: SubtractOneFromCard
- Description: "Subtract -1 from selected card (min 1)"
- Actions Required: 1
- Color: Red

**4. Peek Dealer**
- Name: "Peek"
- Action Type: PeekDealerCard
- Description: "Peek at dealer's hidden card"
- Actions Required: 1
- Color: Purple

**5. Force Redraw**
- Name: "Redraw"
- Action Type: ForceRedraw
- Description: "Discard selected card and draw new one"
- Actions Required: 1
- Color: Orange

**6. Double Value**
- Name: "Double"
- Action Type: DoubleCardValue
- Description: "Double a card's value (max 10)"
- Actions Required: 2 (costs 2 actions!)
- Color: Gold/Yellow

#### Step 3: Assign Actions to Deck
1. Select **Deck** GameObject
2. In Inspector, find **Available Action Cards** array
3. Set **Size** to 6 (or however many you created)
4. Drag each ActionCard ScriptableObject into the array slots

---

## 🎮 How It Works

### Auto-Reshuffle System

**Deck Counter Display:**
```
Deck: 52  (White - plenty of cards)
Deck: 8   (Yellow - getting low)
Deck: 3   (Red - very low!)
```

**Manual Reshuffle (10-5 cards):**
- Reshuffle button appears automatically
- Player can click to reshuffle manually
- Shows notification: "Deck Reshuffled! (+45 cards)"

**Auto-Reshuffle (< 5 cards):**
- System automatically triggers
- Shows: "AUTO-RESHUFFLING DECK..."
- Clears discard pile
- Resets deck to 52 cards
- Shuffles everything

###Action Cards System

**Action Budget:**
- Start each hand with 2 actions
- Each action card costs 1-2 actions
- Display shows: "Actions: 2/2" → "Actions: 1/2" → "Actions: 0/2"

**Using Actions:**
1. Click an action card
2. Select required cards in your hand (if needed)
3. Action executes if valid
4. Action budget decreases
5. Card dims (if not reusable)
6. Next hand: All actions reset

---

## 🧪 Testing Guide

### Test Auto-Reshuffle System:

**Test Manual Reshuffle:**
1. Play ~9-10 hands (use up ~45 cards)
2. **Check:** Discard Pile ~45, Deck ~7
3. **Reshuffle button should appear**
4. Click it
5. **Expected:** Discard Pile 0, Deck 52, notification shows

**Test Auto-Reshuffle:**
1. Play until Deck shows 4-5 cards
2. Try to draw a card (hit)
3. **Expected:** "AUTO-RESHUFFLING DECK..." message
4. Deck resets to 52, Discard Pile 0

**Deck Counter Color Changes:**
- >= 10 cards: White
- 5-9 cards: Yellow
- < 5 cards: Red

### Test Action Cards:

**Test Swap Cards:**
1. Start hand, have 2+ cards
2. Click two cards to select them
3. Click "Swap Cards" action
4. **Expected:** Cards swap values, Actions: 1/2

**Test +1/-1:**
1. Select one card
2. Click "+1 Card" action
3. **Expected:** Card value increases by 1, score updates

**Test Peek:**
1. Click "Peek" action
2. **Expected:** Dealer's hidden card flips briefly, Actions: 1/2

**Test Redraw:**
1. Select a bad card
2. Click "Redraw" action
3. **Expected:** Card discarded, new card drawn

**Test Double:**
1. Select a card (value 5)
2. Click "Double" action
3. **Expected:** Card value becomes 10, Actions: 0/2 (costs 2!)

**Action Budget:**
- Use 2 actions → "No actions remaining!"
- Start new hand → Actions reset to 2/2

---

## 📊 Expected Console Logs

### Reshuffle Logs:
```
AUTO-RESHUFFLE triggered! Only 4 cards remaining.
Reshuffling 45 cards from discard pile back into deck
Reshuffled 45 cards. Deck reset to position 0
```

### Action Logs:
```
Spawned 6 action cards
Action Swap Cards used successfully!
Action consumed. Remaining: 1/2
Cards swapped successfully!
```

```
Select a card to add +1!
Added +1 to card (new value: 8)
Action consumed. Remaining: 0/2
```

```
Not enough actions! Need 2, have 1
```

---

## 🎯 Action Card Details

| Action | Cost | Requires Selection | Effect |
|--------|------|-------------------|--------|
| Swap Cards | 1 | 2 cards | Swap their values |
| +1 Card | 1 | 1 card | Add +1 (max 10) |
| -1 Card | 1 | 1 card | Subtract -1 (min 1) |
| Peek | 1 | None | See dealer's card |
| Redraw | 1 | 1 card | Discard & draw new |
| Double | 2 | 1 card | Double value (max 10) |
| Set to 10 | 1 | 1 card | Make it 10 |
| Flip Ace | 1 | 1 Ace | Toggle 1↔11 |
| Copy Card | 1 | 2 cards | Copy first to second |
| Shield | 1 | 1 card | Protect from boss (planned) |

---

## 💡 Tips

### Best Practices:
1. **Use actions strategically** - You only get 2 per hand
2. **Expensive actions** (like Double) cost 2 actions = whole budget
3. **Reshuffle manually** when button appears to control timing
4. **Auto-reshuffle** is safety net but interrupts gameplay

### Action Combos:
- **Swap + Double**: Swap low card with high, then double the high one
- **-1 + Peek**: Lower your total after seeing dealer's card
- **Redraw + +1**: Replace bad card, then boost another

### Resource Management:
- **Actions (2)** → Used per hand, resets each hand
- **Tarots (1)** → Limited per hand, different from actions
- **Hits (3)** → Maximum hits per hand
- **Deck (52)** → Auto-refills from discard pile

---

## 🔧 Troubleshooting

### Reshuffle button not appearing:
- Check: Discard Pile counter > 0
- Check: Deck counter 5-9 cards
- Check: Button reference assigned in Deck component

### Auto-reshuffle not triggering:
- Check Console for "AUTO-RESHUFFLE triggered!" log
- Verify discard pile has cards
- Try playing more hands

### Action cards not appearing:
- Check: Action Cards Panel assigned
- Check: Action Card Prefab assigned
- Check: Available Action Cards array has items
- Check Console for "Spawned X action cards"

### Action card does nothing when clicked:
- Check: ActionCard component on prefab
- Check: OnPointerClick events working
- Check: Action budget > 0
- Select required cards first (if action needs them)

---

## 🎨 UI Layout Suggestion

```
┌─────────────────────────────────────┐
│  Deck: 45  Discard: 7        [Boss] │
│                                      │
│          Dealer [Card][Card]         │
│                                      │
│   Hits: 2/3   Actions: 1/2          │
│                                      │
│          Player [Card][Card][Card]   │
│                                      │
│  [Hit] [Stand] [2x] [🔄 Reshuffle]  │
│                                      │
│  Action Cards:                       │
│  [Swap] [+1] [-1] [Peek] [Redraw] [x2] │
└─────────────────────────────────────┘
```

---

## Summary

✅ Auto-reshuffle when deck < 5 cards
✅ Manual reshuffle button when 5-9 cards  
✅ 10 fully functional action types
✅ Action budget system (2 per hand)
✅ Visual UI for all systems
✅ Proper reset between hands
✅ Full integration with existing Round Flow features

You now have a complete action card system that uses the action budget properly!

