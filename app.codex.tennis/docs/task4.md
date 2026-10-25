# Editing the auto created schedule
After the schedule is created the user can make changes.
* A user selects a player by press holding the players name.
  * Options are shown:
    1. Pin - means when a schedule is refreshed the player stays in this position.
    2. Swap - means this player will swap with the next player selected. The presses the next player. The two players are swapped (not pinned players).
        2.1 If the swapping of a player means that those two players are playing with each other for the second time then they turn red. Otherwise they are blue.
* There is a button at the bottom of the schedule cards called Re-schedule.
  * Clicking this button attempts to resolve the red conflicted players, using the rules from the spec.
    * NOTE: That if a game/round is marked as complete it can not be changed, so if two players had played together and are now placed together in an unplayed game then they need to be separated. An option dialog is shown where the user selects one of the two players to be moved or accepts the conflict
    * The schedule must still be completed so that other players are given games.

## Dialogs
* The press-and-hold options, the pin confirmation and the "already played together" dialog must fit on the page:
  * Show them in the middle of the screen, never anchored to the bottom where the toolbar and home bar are.
  * The whole dialog (title, message and every button) must be visible on the smallest supported iPhone (iPhone SE, 375 × 667), clear of the notch, the toolbar and the home bar, wherever the page is scrolled to.
  * If the content is taller than the screen, the dialog scrolls inside itself; its buttons must still be reachable.
