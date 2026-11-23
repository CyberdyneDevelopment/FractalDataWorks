@FDW-5663
Feature: Data options table not named
  When the server settings DataOptionsDataStore, DataOptionsPath and DataOptionsContainer do not name an options table, every option row route
  answers its own refusal and not a server error. This feature has no Background on purpose: a Background that lists or clears the rows would
  itself be refused. Run it against a host whose three settings are not set (a second host, or the same host restarted with them unset):
  select it with the tag options-table-not-named and exclude that tag from the run against the host that names a table.

  @FDW-5664 @options-table-not-named
  Scenario: With no options table named, a route answers DATA-121670
    Given the data options table is not named by the server settings
    And a person who holds datastores:read
    When the person lists the rows of the container "Teams"
    Then the answer status is 409
    And the answer carries the code DATA-121670
