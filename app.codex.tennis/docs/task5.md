# Home screen control
create a branch: feature/task5

## UI changes on the home screen
* Replace the Court n text with a stylised tennis ball with the court number in it.
  * The active round, ball is green, non-played round blue, played round grey.
  * The app will be used at night under lights, so the court number must be big and easy to see:
    * The number fills most of the ball, in bold white with a thin dark outline.
    * The ball's seams stay at its edges so they don't cross the number.
    * The ball colours are dark enough that the white number stands out (at least 4.5:1 contrast), including the grey.
* Center the v to be in the middle of the two pairs of players.
  * Spread the pairs out: each pair is centred in its own half of the row, so there's no big gap between the ball and the first pair and the pairs aren't bunched against the v.
* Use the players first name in last name initial Daryl D.

## Round activation
* Any round can be activated regardless of the order.
* On the card header put an action button. When clicked show
  * Start round. Only one round can be started at once.
  * If the round is completed then the action button is always disabled
  * If the round is started but not complete then the action button when clicked shows:
    * Cancel: Marks the round not started
    * Finish: Marks the round as complete.

## Dialogs
* The round action dialog (Start / Finish / Cancel round) must fit on the page:
  * Show them in the middle of the screen, never anchored to the bottom where the toolbar and home bar are.
  * The whole dialog (title, message and every button) must be visible on the smallest supported iPhone (iPhone SE, 375 × 667), clear of the notch, the toolbar and the home bar, wherever the page is scrolled to.
  * If the content is taller than the screen, the dialog scrolls inside itself; its buttons must still be reachable.
