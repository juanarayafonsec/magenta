-- View for player currency balances
CREATE OR REPLACE VIEW v_player_currency_balances AS
SELECT
    c.code AS currency_code,
    n.name AS network,
    a.player_id,
    COALESCE(ab.balance_minor, 0) AS balance_minor,
    COALESCE(ab.cashable_minor, 0) AS cashable_minor,
    COALESCE(ab.reserved_minor, 0) AS reserved_minor
FROM currency_networks cn
JOIN currencies c ON c.currency_id = cn.currency_id
JOIN networks n ON n.network_id = cn.network_id
LEFT JOIN accounts a ON a.currency_network_id = cn.currency_network_id AND a.account_type = 'MAIN'
LEFT JOIN account_balances ab ON ab.account_id = a.account_id
WHERE c.is_active AND n.is_active;




