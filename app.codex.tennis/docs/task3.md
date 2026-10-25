# Schedule page
This page is where a new schedule is created.
If there is no active schedule then the page shows a create schedule button.
When the button is clicked the create new schedule task starts
! Important: This is where the schedule engine is built

## Court Selection
The form shows
* Select Courts: [text box] [Next button]
* Enter the court numbers. This is a text box where the user enters a comma separated list of courts
    EG 4,5,6.
* This implies 3 courts in use and 12 players on all courts at once.
* The user presses next
  * A new schedule is created and the court list is saved.

## User Selection
The form now shows an player search text box. And an add button next to it.
* As the user types in letters after 1 letter the players starts showing names matching those letters (either first or last name). It filters as more letters are entered.
  * The user selects the name and presses the add button
  * That name is added to a list of names under the search box.
    * The list should show the first name, last name and a remove button
  * Under the list of names is a back and next button.
    * If the user clicks back the current list is saved for this schedule and the players are saved, the court selection page is shown
    * If the user clicks next the current list is saved for this schedule and the schedule is created - Adhear to schedule rules in @docs/spec.md
      * While the schedule is being created to page shows matching players
      * When complete the edit/review schedule page is shown

## Review schedule
It is important that this page is laid out so that it is clear to see the rounds, games and player pairs.
This is where users will edit pairs.
Suggested layout
* Each Round is in its own card with a header and the content being the players pairs.
* Each game row shows the player pairs - its not neccary in this page to show the court numbers.
  * Each game must have its own row.
  * It shall be possile to change the order of the games by dragging the entire row up/down - shown with ||. Unless completed
  * It shall be possible to change the order of the rounds. Unless completed.
  EG:
  {                   Round 1                 } ||.    --> round 1
  [player 1] [player 2] V [player 3] [player 4] ||.    --> game 1
  [player 5] [player 6] V [player 7] [player 8] ||.    --> game 2

  {                   Round 2                 } ||.    --> round 2
  [player 1] [player 6] V [player 7] [player 4] ||.    --> game 3
  [player 5] [player 2] V [player 3] [player 8] ||.    --> game 4

* Under the schedule is a Play button and edit button.
  * Play: When clicked the schedule is generated and saved and is visible on the home page.
  * Edit: Will be defined in a future task.
* All games are set to not complete.
* A total of 6 rounds are created

## Showing the schedule on the home screen.
* The same card layout can be shown on the home screen but the court numbers are now included. The first game is allocated to the lowest court number
  * EG, for courts 4,5,6. Game 1 -> court 4, Game 2 -> court 5 etc.
* The cards are read only - no holding / editing players
* There is an edit button at the bottom.
  * When clicked the Review schedule page is opened without a back button.


