# RegexBot
**This branch contains code that is still a major work in progress, and is unusable in its current state. See the master branch for the current working version.**

RegexBot is a self-hosted Discord moderation bot that takes some inspiration from Reddit's Automoderator. It provides a number of features to assist in watching over the tedious details in a busy server with no hidden details, arbitrary restrictions, or unmodifiable behavior. Its configuration allows for a very high level of flexibility, ensuring that the bot behaves in accordance to the exact needs of your server.

### Feature overview for 3.0:
* Modular structure allows for extra features to be written, further enhancing the bot's customizability wherever it may be deployed.
* Versatile JSON-based configuration, support for separate servers.
* High detail logging and record-keeping prevents gaps in moderation that might occur with large public bots.

### Feature overview for RegexBotModule 3.0:
* Create rules based on regular expression patterns
  * Follow up with custom responses ranging from sending a DM to disciplinary action
* Create pattern-based triggers to provide information and fun to your users
  * Adjustable rate limits per-trigger to prevent spam
  * Specify multiple different responses to display at random when triggered
  * Make things interesting by setting triggers that only activate at random
* Individual rules and triggers can be whitelisted or blacklisted per-user, per-channel, or per-role
  * Exemptions to these filters can be applied for additional flexibility

## Documentation
Coming soon.
