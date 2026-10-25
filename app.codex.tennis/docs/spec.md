# Social Tennis: Product Specification

## Overview

Social Tennis schedules social tennis doubles sessions. The organiser keeps a list of players, picks the courts and the players who have turned up, and the app builds the session's rounds. During the session the organiser runs the rounds from the app and can adjust the schedule as people arrive or leave.

The app is used courtside on an iPhone, often at night under lights and often with no signal.

Text in quotes, or shown as **bold** labels, is the exact wording the app uses. `<name>`, `<n>` and similar mark values filled in by the app.

## Platform

- The app is a **Progressive Web App (PWA)** installed on an iPhone from Safari (Share → Add to Home Screen). It must not need the App Store or an Apple developer account.
- The app is called "Social Tennis" (the browser title and install name). On the Home Screen it is called "Tennis" and has a tennis-ball icon.
- Once installed it opens full screen in portrait and works **fully offline**. A connection is only needed for the first install and to pick up new versions.
- It is built with **C# and .NET Blazor WebAssembly**, so the whole app, including schedule generation, runs on the device in the browser.
- There is **no backend server**. All data (players, notes, ranks and schedules) is stored on the phone in browser storage.
- The app is served as static files over HTTPS (GitHub Pages).

## Navigation

- A toolbar at the bottom of the screen has four items, each with an icon, in this order: **Play**, **Players**, **Schedule**, **About**.
- A header at the top shows the name of the current page.
- **Play** is the home page. If no schedule is in play when the app opens, it goes to **Players** instead. Once the user has navigated, tapping Play stays on Play.
- An unknown address shows "Sorry, the content you are looking for does not exist."

## Players

### Player list

- A search box (placeholder "Search players") filters the list as the user types. The search ignores case, and each word typed must appear in the first or last name, so "ann sm" finds Ann Smith.
- A sort drop-down offers "First name (A–Z)", "First name (Z–A)", "Last name (A–Z)" and "Last name (Z–A)". The default is "First name (A–Z)". Ties are broken on the other name. The chosen sort is remembered when the app is closed and re-opened.
- Buttons: **Add player** and **Import**.
- Each player is listed with their full name (first and last) and an **Edit** button.
- Messages:
  - With no players: "No players yet. Add your first player."
  - When the search matches nobody: "No players match “<search>”."
- The list is kept on the phone when the app is closed and re-opened, and when the app is updated.

### Adding and editing a player

- The page heading is "Add player" or "Edit player", with a "‹ Players" link back to the list.
- Fields:
  - **First name** (required, up to 50 characters).
  - **Last name** (optional, up to 50 characters).
  - **Rank**: a drop-down of None, 1, 2 or 3.
  - **Comment**: a multi-line text box, up to 500 characters.
- Validation messages:
  - "First name is required."
  - "First name must be 50 characters or fewer."
  - "Last name must be 50 characters or fewer."
  - "Rank must be between 1 and 3."
  - "Comment must be 500 characters or fewer."
- **Save** saves the player and returns to the list.
- When editing, a **Delete player** button opens a confirmation:
  - Title "Delete player?"
  - Message "<full name> will be removed. This can't be undone."
  - Buttons **Cancel** and **Delete**. Deleting returns to the list.
- If the player no longer exists, the page shows "This player no longer exists."
- Ranks above 3 stored by an older version are treated as 3.

### Import

- **Import** opens a dialog titled "Import players" with the help text "One player per line: first name, last name and an optional rank (1–3), separated by commas."
- The text box accepts players in any of these formats. Any value may be in double quotes, and `""` inside quotes is a literal quote.
  ```csv
  firstName, lastName
  "first Name", "last Name"
  firstName, lastName, rank
  "first Name", "last Name", "rank"
  ```
- When the text box is empty, the placeholder shows:
  ```
  One player per line, e.g.
  Daryl, Smith
  "Mary Ann", "Jones"
  Sam, Lee, 2
  "Jo", "De Silva", "3"
  ```
- If a line can't be read, the offending part is underlined with a red squiggle and an error is listed in the form "Line <n>: <problem>". The problems are:
  - "a quote is not closed."
  - "there is text after a closing quote."
  - "quotes must go around the whole value."
  - "enter a first name and a last name, separated by a comma."
  - "too many values: only first name, last name and rank."
  - "the first name is missing."
  - "names must be 50 characters or fewer."
  - "the rank must be 1 to 3 (or left out)."

  Only the first few errors are listed, followed by "…and <n> more." if there are others.
- When every line can be read, the dialog shows "<n> players ready to import." ("1 player" for one).
- Buttons: **Cancel** and **Save**. **Save** is disabled until every line can be read and there is at least one player.
- Players already in the list (same first and last name, ignoring case) are skipped. After importing, the Players page shows:
  - "Imported <n> players." or
  - "Imported <n> players (<m> already here)."
- The whole dialog must be visible on the screen.

### Backup and restore

- Players must never be lost when the app is updated. A **Backup** section on the Players page explains: "Players are stored on this phone. Save a backup now and then so you can restore them if the app is ever removed."
- Buttons: **Save backup** (disabled when there are no players) and **Restore from backup**.
- The backup is a JSON file named `social-tennis-players-<yyyy-MM-dd>.json`.
- Backups are kept in a folder called `social.tennis.app` in the user's Documents folder:
  - **Where the browser can create and open folders** (such as Chrome or Edge on a computer):
    - The page explains: "Backups are kept in a social.tennis.app folder. The first time, choose Documents and the folder is created there."
    - The first save asks for Documents and creates the folder. Later saves and restores open it directly.
    - After saving, the page shows "Backup of <n> players saved in social.tennis.app."
  - **Where it can't (Safari on iPhone)**:
    - The page explains: "Keep backups in a social.tennis.app folder in Documents: choose Save to Files, open Documents, and create the folder the first time. To restore, open that folder (or wherever you saved the file)."
    - Saving opens the share sheet (Save to Files, AirDrop, email and so on), or downloads the file where there is no share sheet.
    - Restoring opens the file picker.
    - After saving, the page shows "Backup of <n> players saved."
- Restoring adds the players from the backup and skips any already in the list (same player, or same first and last name). Afterwards the page shows:
  - "Restored <n> players." or
  - "Restored <n> players (<m> were already here)."
- A file that isn't a backup is rejected with "That file isn't a Social Tennis players backup." A file that is too big is rejected with "That file is too large to be a players backup."

## Creating a schedule

Only one schedule exists at a time. When there is none, the Schedule page shows "There is no active schedule." and a **Create schedule** button, which starts the steps below. The schedule is saved after every step, so if the app is closed it resumes at the same step.

### Step 1: Courts

- The heading is "Courts". A text box labelled **Select courts** (placeholder "e.g. 4,5,6", hint "Enter the court numbers, separated by commas.") takes a comma-separated list such as `4,5,6`. That means three courts and up to 12 players on court at once.
- **Next** (or the Enter key) saves the courts and goes to step 2.
- Errors:
  - "Enter at least one court number, for example 4,5,6."
  - "“<entry>” is not a court number."
  - "Court numbers must be 1 or more."
  - "Court <n> is listed more than once."

### Step 2: Players

- The heading is "Players". If there are no players at all, the page shows "No players yet. Add players first, then come back.", where "Add players" links to the Players page.
- A search box labelled **Add players** (placeholder "Type a name") has an **Add** button next to it.
  - From the first letter typed, up to 8 players whose first or last name matches are suggested, narrowing as more letters are typed. Players already added aren't suggested.
  - If nothing matches, the page shows "No matching players."
  - The user picks a suggestion and presses **Add**.
- The players added are listed under the heading "Playing (<n>)". Each row shows the first name, the last name and a **Remove** button. Before anyone is added, the list says "Nobody added yet."
- Until there are 4 players, the page shows "Add at least 4 players." and **Next** is disabled.
- **Back** saves the list and returns to step 1.
- **Next** saves the list and generates the schedule. While it is being generated the page shows "Matching players…" and the buttons are disabled. When it is done, the review page is shown.

## Schedule rules

A session has **6 rounds**. In each round, every player is either in exactly one game or sitting out.

1. **Partners never repeat.** No two players are partners more than once in a session. If 6 rounds are impossible without a repeat, the schedule has as many rounds as possible. For example, 4 players have only 3 possible pairings, so they get 3 rounds, and 5 players get 5 rounds. The review page then says "With <n> players only <r> rounds are possible without anyone partnering the same player twice."
2. **At least 4 players** are needed to create a schedule.
3. **Doubles first.** Each round fills as many courts as possible with doubles (4 players per court).
4. **Leftover players:**
   - If 3 players are left over and a court is free, they play a **three-player game** (one player against a pair).
   - If 2 are left over and a court is free, they play **singles**.
   - Anyone else (1 left over, or more players than the courts hold) **sits out** that round.
5. **Fair shares.** Sitting out, three-player games and singles are each shared out evenly across the session: nobody has more than one more of each than anyone else.
6. **Mixed opponents.** Players should face as many different opponents as possible. Nobody should meet the same opponent more than twice in a session. Where the other rules make that impossible (for example 6 players with singles), nobody meets the same opponent more than three times.
7. **Ranks in singles.** A rank 1 player doesn't play a rank 3 in singles. Only if the schedule can't otherwise be completed (for example, a lone rank 1 among rank 3s) may a 1 play a 3, as few times as possible, and that game is shown in red.
8. **Court order.** In each round, the first game is on the lowest-numbered court, the second game on the next, and so on. For courts 4, 5 and 6, game 1 is on court 4 and game 2 is on court 5.

Generating a schedule must be quick enough to use courtside, even for a large session (for example 24 players on 6 courts).

### Example session (illustration)

| Game | Court | Team 1           | v | Team 2           | Complete |
|------|-------|------------------|---|------------------|----------|
| 1    | 1     | Player1, Player5 |   | Player7, Player8 | Y        |
| 2    | 2     | Player2, Player3 |   | Player4, Player6 |          |
| 3    | 1     | Player4, Player7 |   | Player6, Player8 |          |
| 4    | 2     | Player1, Player2 |   | Player3, Player5 |          |

## Schedule cards

The review page and the Play page both show the schedule as cards:

- Each round is a card with a header "Round <n>" and a row for each game.
- Each game row shows the two sides with a "v" centred between them. Each side is centred in its own half of the row, and names are left justified. In a three-player game the single player is on the left and the pair on the right.
- Players sitting out are listed under the games: "Sitting out: <names>".
- Names are shown as first name and last-name initial (for example "Daryl D."). If two players would then look the same, their full names are shown.
- Pinned players have a pin icon after their name.

## Reviewing and editing the schedule (Schedule page)

### Review

- The heading is "Review schedule". The page makes it easy to see the rounds, the games and who partners whom. It doesn't show court numbers.
- Each name has a small coloured dot for the player's rank: grey for none, green for 1, yellow for 2 and red for 3.
- Outside edit mode, each round and game that isn't complete has a drag handle (||). Dragging a round's handle moves the whole round, and dragging a game's handle moves the game within its round. Completed rounds and games can't be moved.
- Before the schedule has been played, and outside edit mode, a "‹ Players" link returns to step 2.
- Under the cards are **Edit** (shown as **Done** while editing) and **Play**.
- **Play** leaves edit mode, saves the schedule, marks it as in play and opens the Play page.
- A newly created schedule opens in **edit mode** and stays in it, even if the app is closed, until the user taps **Done** or **Play**.

### Edit mode

- In edit mode the drag handles are hidden, and a banner says "Press and hold a name to pin or swap a player." The banner also shows the result of the last action.
- **Press and hold** a player's name (about half a second) to open a dialog titled with the player's full name. Its message and options depend on where the player is:

  | Player | Message | Options |
  |---|---|---|
  | In a game | "Round <n>." | **Pin**, **Swap**, **Remove from schedule** |
  | Pinned | "Pinned in this game: re-scheduling won't move them and they can't be swapped." | **Unpin**, **Remove from schedule** |
  | Sitting out | "Sitting out in round <n>." | **Swap**, **Remove from schedule** |
  | In a completed game | "This game is complete, so it can't be changed." | none |
  | In a completed round | "Round <n> is complete, so nothing in it can be changed." | none |

  The dialog's last button is **Cancel**, or **Close** when there are no options.
- **Pin:** the player keeps this position whenever the schedule is re-calculated. Pinning needs no confirmation. Only the user can unpin a player.
- **Swap:**
  - The banner says "Tap a player in round <n> to swap with <name>." and shows a **Cancel** button.
  - The user taps another player in the same round, and the two swap places. Swaps are only within a round, because a player can't be in two games in one round. A player who is sitting out can be swapped into a game.
  - Swaps that aren't allowed show:
    - "Pick a player in round <n>."
    - "<name> is pinned and can't be swapped."
    - "That game is complete, so it can't be changed."
    - "<name> and <name> are already on the same side."
- **Remove from schedule** opens a confirmation:
  - Title "Remove <full name>?"
  - Message "They are taken out of <rounds> and the schedule is re-arranged.", followed by " Completed rounds keep them." when some rounds are complete. When every round is complete, the message is "They are taken out of the schedule. Every round is complete, so the rounds don't change."
  - Buttons **Cancel** and **Remove**.
  - The player is taken out of every round that isn't complete and those rounds are re-arranged. Completed rounds keep them.
  - Results: "Removed <name> from <rounds>." or "Removed <name>.". A schedule can't go below 4 players: "A schedule needs at least 4 players, so <name> can't be removed."
- **Add a player:** a search box labelled **Add a player to the schedule** works like step 2.
  - The new player is added to every round that isn't complete, the rounds are re-arranged for the new number of players, and sitting out, three-player games and singles are shared out fairly again. A late arrival counts the rounds they missed as sitting out, so they get to play.
  - Where they play is shown in blue, with the result "Added <name> to <rounds> (shown in blue)."
  - Other results: "Every round is complete, so there's nowhere to add <full name>." and "<full name> is already in the schedule."
- Rounds in messages are written "round 3", "rounds 3–6" or "rounds 2, 4, 5".
- **Colours:**
  - Players who become partners for a second time are shown in **red**.
  - Players moved by the user, by Re-schedule, or placed by adding a player are shown in **blue**.
  - **Done** (and **Play**) clears the blue markers.
- **Re-schedule** is a button under the cards, shown as "Re-scheduling…" while it runs. It resolves the red conflicts using the schedule rules, with as few changes as possible:
  - It only moves players within a round, never moves pinned players or anything in a completed round, and prefers not to undo the user's own changes.
  - When a user's swap creates a repeat partnership, it fixes the repeat in the other round where the pair are partners (the one the user didn't edit), if it can.
  - It keeps the fairness and opponent rules, so that everyone still gets their games.
  - Results:
    - "Re-scheduled: <n> players moved (shown in blue)."
    - "Nothing needed changing."
    - "Couldn't separate <name> & <name>: pinned players are in the way."
  - When adding or removing a player leaves a repeat that pinned players block, the result adds " Some partners still play together twice (red): pinned players are in the way."
- **Completed-game conflicts:** if a repeat partnership involves a **completed** game (which can't change), a dialog appears:
  - Title "Already played together"
  - Message "<full name> and <full name> were partners in round <n>, which is complete, and are partners again in round <m>. Who should move?"
  - Options **Move <first player>**, **Move <second player>**, **Keep them together** and **Cancel**.
  - **Keep them together** accepts the repeat: the pair are no longer shown in red, and Re-schedule leaves them together.
- Nothing in a completed round can be edited: no pinning, unpinning, swapping, adding, removing or reordering.

## Play page

- Shows the schedule in play as cards with court numbers. It is read only: players can't be changed here, there is no Edit button, and there are no links to editing. Rank dots are not shown.
- Messages when there is no schedule in play:
  - With no schedule: "No schedule in play." and "Create one on the Schedule page."
  - With a schedule still being set up: "No schedule in play." and "Finish setting it up on the Schedule page, then tap Play."
- Each game's court is shown as a **stylised tennis ball** with the court number in it:
  - **Blue** means the round hasn't started, **green** means it is in play, and **grey** means it is complete.
  - The number fills most of the ball, in bold white with a thin dark outline. The ball's seams stay at its edges so they don't cross the number.
  - The ball colours are dark enough that the white number has at least 4.5:1 contrast, the grey included, so it can be read at night.
- Each round card's header shows the round's status ("In play" or "Complete"; nothing when not started) and an action button (⋮). The button opens a dialog titled "Round <n>" with a **Close** button:
  - **Not started:** message "Not started yet." with **Start round**. Any round can be started, in any order, but only one round can be in play at a time. While another round is in play, the message is "Round <n> is in play. Finish or cancel it before starting another." and Start round is disabled.
  - **In play:** message "In play." with **Finish round**, which marks every game in the round complete, and **Cancel round**, which returns it to not started.
  - **Complete:** the action button is disabled.
- Under the schedule is a **Close schedule** button, which opens a confirmation:
  - Title "Close this schedule?"
  - Message "The schedule and all its rounds will be deleted. Your players are kept. This can't be undone."
  - Buttons **Cancel** and **Close schedule**.
  - After closing, the Schedule page shows Create schedule again.

## About page

- Shows "Social Tennis", "Version <version>" with the commit in brackets, and "Released <d Month yyyy>". A local build shows "Local build" instead of a release date.
- The version is set automatically every time the app is deployed: `1.0.<deploy number>`, together with the commit and the release date.
- A **Theme** choice of **Light**, **Dark** or **Device** (follow the phone's setting, which is the default). The choice is remembered on the phone and applied as the app opens.
- **How to use**, with brief numbered instructions:
  - **Players:** add everyone once (or Import a list); names are remembered on the phone.
  - **Schedule:** Create schedule, enter the court numbers, add today's players by typing the first letters of their names.
  - **Review:** six rounds with no repeat partners; leftover players play a three-player game or singles; drag (||) to reorder; Edit to add a player; press and hold to pin, unpin, swap or remove; Re-schedule fixes red clashes.
  - **Play:** Play shows the schedule on the Play page; each round's ⋮ button starts, finishes or cancels it; blue, green and grey balls.
  - **Finish:** Close schedule on the Play page when the session is over; players are kept.

  The instructions end with a note that the app works offline once installed (Safari → Share → Add to Home Screen) and a reminder to save a backup from the Players page now and then.

## Dialogs

All dialogs (confirmations, player options, the already-played-together choice, round actions and import):

- Appear in the middle of the screen, never anchored to the bottom where the toolbar and home bar are.
- Are fully visible (title, message and every button) on the smallest supported iPhone (iPhone SE, 375 × 667), clear of the notch, the toolbar and the home bar, wherever the page is scrolled to.
- Scroll inside themselves if their content is taller than the screen, with their buttons still reachable.

## General UI

- The interface is clean and uncluttered. It doesn't need to look like a native app.
- Light and dark themes are both supported, and form controls follow the theme.
- While data is loading, pages show "Loading…".
