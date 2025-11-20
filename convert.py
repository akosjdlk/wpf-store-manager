import aiomysql
import asyncio
import aiofiles


async def main():
    db: aiomysql.Connection = await aiomysql.connect(
        host="db-par-02.apollopanel.com",
        user="u208217_G0LNIAe1KA",
        password="sd5JqlpVljm+EOT=IEzta6np",
        db="s208217_akos",
        echo=True
    )
    print("Connected to database")
    cur: aiomysql.Cursor = await db.cursor()
    await cur.execute(
        """
        CREATE TABLE products (
            id               INT UNSIGNED NOT NULL,
            name             VARCHAR(255) NOT NULL,
            unit             VARCHAR(50) NOT NULL,
            supplier_price   FLOAT NOT NULL,
            sale_price       FLOAT NOT NULL,
            vat_percentage   INT NOT NULL,
            stock            FLOAT NOT NULL,
            fractionable     BOOLEAN NOT NULL DEFAULT 0,
            last_modified    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                                ON UPDATE CURRENT_TIMESTAMP,

            PRIMARY KEY (id)
        );
        """
    )
    
    async with aiofiles.open("PECIKK.csv", mode="r", encoding="utf-8") as f:
        lines = await f.readlines()
        lines = lines[1:]  # Skip header line

        # CIKKSZAM, MEGN, MENYEGY, BESZAR, ELADAR, AFASZAZ, AKTKESZ, TORT
        for cikk in lines:
            cur: aiomysql.Cursor = await db.cursor()
            cikk, megnev, kiszer, beszar, eladar, afaszaz, keszlet, tort = cikk.strip().split(";")

            await cur.execute(
                """
                INSERT INTO products (
                    id, name, unit, supplier_price, sale_price, vat_percentage, stock, fractionable
                ) VALUES (
                    %s, %s, %s, %s, %s, %s, %s, %s
                );
                """,
                [
                    int(cikk),
                    megnev,
                    kiszer,
                    float(beszar.replace(",", ".")),
                    float(eladar.replace(",", ".")),
                    int(afaszaz.replace(",", ".")),
                    max(0, float(keszlet.replace(",", "."))),
                    True if tort == "TRUE" else False
                ]
            )
            
            await cur.close()


asyncio.run(main())