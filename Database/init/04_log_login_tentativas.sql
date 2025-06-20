CREATE TABLE IF NOT EXISTS logLoginTentativas (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Login VARCHAR(50) NOT NULL,
    Sucesso TINYINT(1) NOT NULL,
    IpAddress VARCHAR(45) NOT NULL,
    UserAgent TEXT NULL,
    DataTentativa DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Índices
    INDEX idx_logLogin_Login (Login),
    INDEX idx_logLogin_DataTentativa (DataTentativa),
    INDEX idx_logLogin_Sucesso (Sucesso),
    INDEX idx_logLogin_IpAddress (IpAddress)
);