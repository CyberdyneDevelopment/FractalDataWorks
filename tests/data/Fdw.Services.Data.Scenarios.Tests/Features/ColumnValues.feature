@FDW-5663
Feature: Column values search
  A person who can read a data store container finds the values a column holds without loading all of them:
  the store searches, counts and pages, and the answer says whether more values match.

  Background:
    Given a person who holds datastores:read
    And a container whose column "Division" holds these values
      | value | rows |
      | East  | 3    |
      | West  | 2    |
      | North | 4    |
      | South | 1    |
      | Eastern Shore | 5 |

  @FDW-5666
  Scenario: A search narrows the values to those that contain the text
    When the person searches the column for "East"
    Then the answer status is 200
    And the values answered are "East" and "Eastern Shore"
    And every value carries the number of rows that hold it

  @FDW-5666
  Scenario: The search text is matched literally
    When the person searches the column for "%"
    Then the answer status is 200
    And no value is answered

  @FDW-5666
  Scenario: A page that is not the last says that more values match
    When the person asks for 2 values from the column starting at 0
    Then the answer status is 200
    And 2 values are answered
    And the answer says more values match

  @FDW-5666
  Scenario: The last page says that no more values match
    When the person asks for 2 values from the column starting at 4
    Then the answer status is 200
    And 1 value is answered
    And the answer says no more values match

  @FDW-5666
  Scenario: Chosen values are marked
    When the person asks for the values of the column with "West" chosen
    Then the answer status is 200
    And the value "West" is marked chosen
    And the value "East" is not marked chosen

  @FDW-5666
  Scenario: Chosen values can be left out
    When the person asks for the values of the column with "West" chosen and left out
    Then the values answered do not include "West"

  @FDW-5666
  Scenario: A take above the maximum is refused with DATA-21660
    When the person asks for more values than the maximum a page may hold
    Then the answer status is 400
    And the answer carries the code DATA-21660

  @FDW-5666
  Scenario: A take below 1 is refused with DATA-21661
    When the person asks for 0 values from the column starting at 0
    Then the answer status is 400
    And the answer carries the code DATA-21661

  @FDW-5666
  Scenario: A negative skip is refused with DATA-21662
    When the person asks for 2 values from the column starting at -1
    Then the answer status is 400
    And the answer carries the code DATA-21662

  @FDW-5666
  Scenario: An empty chosen value is refused with DATA-21663
    When the person asks for the values of the column with an empty value chosen
    Then the answer status is 400
    And the answer carries the code DATA-21663

  @FDW-5666
  Scenario: A column the container has not discovered is refused with DATA-31660
    When the person searches the column "NoSuchColumn" for "East"
    Then the answer status is 404
    And the answer carries the code DATA-31660

  @FDW-5666
  Scenario: A person without datastores:read is refused
    Given a person who holds no permission
    When the person searches the column for "East"
    Then the answer status is 403
