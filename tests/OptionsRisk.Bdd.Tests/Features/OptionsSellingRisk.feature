Feature: Educational NIFTY options selling risk
  In order to learn risk management without live orders
  As a simulator user
  I want reproducible positions, hedges, exits and net P/L

  Scenario: Open the strategy from spot
    Given NIFTY spot is 25013 and the strike step is 50
    When the user opens the strategy
    Then short positions exist for 25000 CE, 25000 PE, 25500 CE and 24500 PE

  Scenario: Upward spike triggers a hedge suggestion
    Given the strategy is open and the run uses a fixed seed
    When NIFTY spikes up 2 percent
    Then implied volatility rises
    And the coach suggests buying NIFTY futures with the lots needed to bring net delta within threshold
    And it shows projected P/L with and without the hedge

  Scenario: Leg stop-loss
    Given the 25500 CE was sold at 40
    When its price reaches 80
    Then the coach recommends exiting the 25500 CE
    When the user exits it
    Then its realized P/L is (40 - 80) times lot size
    And overall P/L updates

  Scenario: Overall P/L
    Given the strategy has open legs, closed legs and a futures hedge
    Then overall P/L equals the sum of realized and unrealized P/L less cash fees
