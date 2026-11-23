@FDW-5663
Feature: Data option rows
  A person who can write to a data store chooses which values of a column a load imports. Each choice is a row that names a column,
  a value, an option type and an option, and is switched on or off. Reading the rows needs datastores:read; changing them needs datastores:write.
  A refused call changes nothing.

  Background:
    Given the data options table is named by the server settings
    And the container "Teams" has no option rows
    And the container "Teams" has the columns "Division" and "Conference"

  @FDW-5664
  Scenario: Rows list in column order, then value order, with a page
    Given a person who holds datastores:write
    And the container "Teams" has these option rows
      | column     | value | optionType | option  | active |
      | Division   | West  | SelectList | Include | true   |
      | Conference | AFC   | SelectList | Include | true   |
      | Division   | East  | SelectList | Exclude | true   |
    When a person who holds datastores:read lists the rows of the container "Teams"
    Then the answer status is 200
    And the rows answered are in this order
      | column     | value |
      | Conference | AFC   |
      | Division   | East  |
      | Division   | West  |
    And every row carries its id

  @FDW-5664
  Scenario: Rows list for one column
    Given a person who holds datastores:write
    And the container "Teams" has these option rows
      | column     | value | optionType | option  | active |
      | Division   | East  | SelectList | Include | true   |
      | Conference | AFC   | SelectList | Include | true   |
    When a person who holds datastores:read lists the rows of the container "Teams" for the column "Division"
    Then the rows answered are in this order
      | column   | value |
      | Division | East  |

  @FDW-5664
  Scenario: Several rows are added in one call
    Given a person who holds datastores:write
    When the person adds these rows to the container "Teams"
      | column   | value | optionType | option  | active |
      | Division | East  | SelectList | Include | true   |
      | Division | West  | SelectList | Exclude | false  |
    Then the answer status is 201
    And 2 rows are answered, each with an id
    And the container "Teams" holds 2 option rows

  @FDW-5664
  Scenario: An add with no rows is refused with DATA-21670
    Given a person who holds datastores:write
    When the person adds no rows to the container "Teams"
    Then the answer status is 400
    And the answer carries the code DATA-21670

  @FDW-5664
  Scenario: An add with an empty value is refused with DATA-21671
    Given a person who holds datastores:write
    When the person adds a row with the value "" to the container "Teams"
    Then the answer status is 400
    And the answer carries the codes DATA-21671 and DATA-21676
    And the container "Teams" holds 0 option rows

  @FDW-5664
  Scenario: An add with a column and value twice is refused with DATA-21672
    Given a person who holds datastores:write
    When the person adds the column "Division" and the value "East" twice to the container "Teams"
    Then the answer status is 400
    And the answer carries the codes DATA-21672 and DATA-21676
    And the container "Teams" holds 0 option rows

  @FDW-5664
  Scenario: An add with a row that has no active is refused with DATA-21677
    Given a person who holds datastores:write
    When the person adds a row that does not state active to the container "Teams"
    Then the answer status is 400
    And the answer carries the codes DATA-21677 and DATA-21676
    And the container "Teams" holds 0 option rows

  @FDW-5664
  Scenario: An add of a column and value already stored is refused with DATA-41670
    Given a person who holds datastores:write
    And the container "Teams" has these option rows
      | column   | value | optionType | option  | active |
      | Division | East  | SelectList | Include | true   |
    When the person adds the column "Division" and the value "East" to the container "Teams"
    Then the answer status is 409
    And the answer carries the code DATA-41670
    And the container "Teams" holds 1 option rows

  @FDW-5664
  Scenario: An add with an unknown option type is refused with DATAOPTION-31000
    Given a person who holds datastores:write
    When the person adds a row with the option type "NoSuchType" to the container "Teams"
    Then the answer status is 404
    And the answer carries the codes DATAOPTION-31000 and DATA-21676
    And the container "Teams" holds 0 option rows

  @FDW-5664
  Scenario: An add with an unknown option is refused with DATAOPTION-30000
    Given a person who holds datastores:write
    When the person adds a row with the option "NoSuchOption" to the container "Teams"
    Then the answer status is 404
    And the answer carries the codes DATAOPTION-30000 and DATA-21676
    And the container "Teams" holds 0 option rows

  @FDW-5664
  Scenario: An add with a column the container has not discovered is refused with DATA-31670
    Given a person who holds datastores:write
    When the person adds a row with the column "NoSuchColumn" to the container "Teams"
    Then the answer status is 404
    And the answer carries the codes DATA-31670 and DATA-21676
    And the container "Teams" holds 0 option rows

  @FDW-5664
  Scenario: A refused row in an add writes nothing
    Given a person who holds datastores:write
    When the person adds one good row and one row with the option type "NoSuchType" to the container "Teams"
    Then the answer status is 404
    And the answer names row 2 with DATA-21676
    And the container "Teams" holds 0 option rows

  @FDW-5664
  Scenario: A row is changed by its id
    Given a person who holds datastores:write
    And the container "Teams" has these option rows
      | column   | value | optionType | option  | active |
      | Division | East  | SelectList | Include | true   |
    When the person changes the option of the row "East" to "Exclude" and switches it off
    Then the answer status is 200
    And the row "East" answers the option "Exclude" and active false

  @FDW-5664
  Scenario: A change that states nothing is refused with DATA-21673
    Given a person who holds datastores:write
    And the container "Teams" has these option rows
      | column   | value | optionType | option  | active |
      | Division | East  | SelectList | Include | true   |
    When the person changes the row "East" and states no change
    Then the answer status is 400
    And the answer carries the code DATA-21673

  @FDW-5664
  Scenario: A change to an empty new value is refused with DATA-21674
    Given a person who holds datastores:write
    And the container "Teams" has these option rows
      | column   | value | optionType | option  | active |
      | Division | East  | SelectList | Include | true   |
    When the person changes the value of the row "East" to ""
    Then the answer status is 400
    And the answer carries the code DATA-21674

  @FDW-5664
  Scenario: A change of a row from another container is refused with DATA-31671
    Given a person who holds datastores:write
    And the container "Players" has these option rows
      | column | value | optionType | option  | active |
      | Name   | Ada   | SelectList | Include | true   |
    When the person changes the row "Ada" through the container "Teams"
    Then the answer status is 404
    And the answer carries the code DATA-31671

  @FDW-5664
  Scenario: Several rows are removed in one call
    Given a person who holds datastores:write
    And the container "Teams" has these option rows
      | column   | value | optionType | option  | active |
      | Division | East  | SelectList | Include | true   |
      | Division | West  | SelectList | Include | true   |
      | Division | North | SelectList | Include | true   |
    When the person removes the rows "East" and "West"
    Then the answer status is 204
    And the container "Teams" holds 1 option rows

  @FDW-5664
  Scenario: A remove with ids from another container is refused with DATA-31672 and removes nothing
    Given a person who holds datastores:write
    And the container "Teams" has these option rows
      | column   | value | optionType | option  | active |
      | Division | East  | SelectList | Include | true   |
    And the container "Players" has these option rows
      | column | value | optionType | option  | active |
      | Name   | Ada   | SelectList | Include | true   |
    When the person removes the rows "East" and "Ada" through the container "Teams"
    Then the answer status is 404
    And the answer carries the code DATA-31672
    And the container "Teams" holds 1 option rows

  @FDW-5664
  Scenario: A remove with no ids is refused with DATA-21675
    Given a person who holds datastores:write
    When the person removes no rows from the container "Teams"
    Then the answer status is 400
    And the answer carries the code DATA-21675

  @FDW-5664
  Scenario: A person with datastores:read can list but a write is forbidden
    Given a person who holds datastores:read
    When the person adds a row to the container "Teams"
    Then the answer status is 403
    And the container "Teams" holds 0 option rows
