@FDW-5663
Feature: Data option types
  A person who can read a data store sees which kinds of option a column can carry, read from the option type collections.
  A client builds its pickers from this answer, so a new option type appears without a change to the client.

  @FDW-5665
  Scenario: The option types answer with their options in apply order
    Given a person who holds datastores:read
    When the person asks for the data option types
    Then the answer status is 200
    And the answer lists the option type "SelectList" with the options "Include" and "Exclude" in that order

  @FDW-5665
  Scenario: A person without datastores:read is refused
    Given a person who holds no permission
    When the person asks for the data option types
    Then the answer status is 403
