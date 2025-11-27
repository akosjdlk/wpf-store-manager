CREATE TABLE IF NOT EXISTS products (
    id INT NOT NULL AUTO_INCREMENT,
    name VARCHAR(255) NOT NULL,
    unit VARCHAR(50) NOT NULL,
    supplier_price DECIMAL(10,2) NOT NULL,
    sale_price DECIMAL(10,2) NOT NULL,
    vat_percentage INT NOT NULL,
    stock DECIMAL(10,3) NOT NULL,
    fractionable TINYINT(1) NOT NULL DEFAULT 0,
    last_modified DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP 
                    ON UPDATE CURRENT_TIMESTAMP,

    PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS barcodes (
    product_id INT NOT NULL,
    code VARCHAR(13) NOT NULL,

    PRIMARY KEY (code),
    FOREIGN KEY (product_id) REFERENCES products(id)
        ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS users (
    id INT NOT NULL AUTO_INCREMENT,
    can_access_storage TINYINT(1) NOT NULL DEFAULT 0,
    password VARCHAR(255) NOT NULL,

    PRIMARY KEY (id)
);