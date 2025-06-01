CREATE TABLE IF NOT EXISTS log_login_tentativas (
    id INT AUTO_INCREMENT PRIMARY KEY,
    login VARCHAR(50) NOT NULL,
    sucesso TINYINT(1) NOT NULL,
    ip_address VARCHAR(45) NOT NULL,
    user_agent TEXT NULL,
    data_tentativa DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_login (login),
    INDEX idx_data_tentativa (data_tentativa),
    INDEX idx_sucesso (sucesso)
);

