# General clean up
branch: feature/ui_clean_up1

## Schedule Page
* Only one schedule can exist at once. When there is an active schedule on the home page, underneath the schedule is a close schedule button.
* If the user clicks it then the a dialog is shown to confirm the deletion. The schedule is deleted.
* When the user goes to the schedule page, then Create schedule is shown. There is no new schedule button.
* When the edit button is clicked it should also be possible to add players into the schedule. The add player mode is shown.
* When the schedule is first created it is in edit mode until Done or Play is clicked.
* Player names on the schedule cards are left justified.

## About page
Add an About page with menu at the bottom.
The about page shows the app version - which is changed when the app is deployed. Make a hook to do this automatically.
Include brief instructions on how to use the app.

* The feature is press hold, then select pin. A pinned players position can not be changed when the schedule is re-calculated.
* If a player is pinned, then the player action (hold press) shows unpin.
* The press hold player should also allow the player to be deleted.
  * If the schedule is started, players can not be added or removed from completed rounds.


When a round is completed nothing in that round is editable.

The player rank option are none, 1-3

In the player search field for adding them to a schedule, I've changed
* As the user types in letters after 1 letter the players starts showing names matching those letters (either first or last name). It filters as more letters are entered.

When the edit button is clicked it should also be possible to

## Player backup
* Make it possible that players names are not lost when the app is updated.
* Create a folder called social.tennis.app in the documents folder (if documents exists). Save backups there.
  * Restore backups from social.tennis.app. If it does not exist then the user needs to navigate to their own folder.

## Home page
Remove the edit button and navigation from home.

## Play page
* The Game page is renamed Play: the menu item, page title and all code names (e.g. the `Home` page component) use Play.
* The rank dots are not shown on the Play page.
* Player names are left justified.

## Players
* Add an import button. When clicked a text area in a dialog is opened. Ensure the entire dialog is visble.
* There should be a save and cancel button.
* The user can paste or enter a comma separated list of names.
  * The format can be one of:
    ~~~csv
    firstName, lastName
    "first Name", "last Name"
    firstName, lastName, rank
    "first Name", "last Name", "rank"
    ~~~
  * An empty text area should show placeholder text showing the entry options.
  * If there is a syntax (illegal) entry, put red squiggle lines under the offending input. The save button should be disabled and a error message shown.

## Menu
* Reorder and rename the menu items to be
  - Play, Players, Schedule, Timer, About. If there is no active game then when the app starts go to players.

## Schedule
* When a re-schedule task is run and there is are players playing with each other more than once, correct the round where the manual swap players was not done - if possible.
* Add a little coloured dot in front of the player's name to indicate rank: none = grey, 1 = green, 2 = yellow, 3 = red. The dots are shown on the Schedule page only, not on the Play page.
* Ensure that in a singles pairing a 1 does not play against a 3.

## Game
* When there are two players left over after a schedule. Add them to the extra court and make it a singles match
  * Ensure that everyone gets a chance to play singles so that not the same person is playing.

## About
* Add and option to select light, dark, device theme

Remove the timer page and menu option.
