using Microsoft.Playwright;
using System.Text;


class Program
{
    // 🛡️ ฟังก์ชันสำหรับ "สุ่มหน่วงเวลา" (เลียนแบบพฤติกรรมมนุษย์)
    static Random rnd = new Random();
    static async Task RandomDelay(int minMs = 1000, int maxMs = 3000)
    {
        int delay = rnd.Next(minMs, maxMs);
        Console.WriteLine($"   [หน่วงเวลาแบบสุ่ม: {delay} มิลลิวินาที...]");
        await Task.Delay(delay);
    }
    public static async Task Main()
    {
        // ==========================================
        // 🛡️ ตั้งค่าเบราว์เซอร์เพื่อพรางตัว (Anti-Detection)
        // ==========================================
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false, // 🎯 สำคัญ: ต้องเป็น false เพื่อเปิดหน้าจอ
            Args = new[] { "--disable-blink-features=AutomationControlled" } // 🎯 ซ่อนสถานะบอท
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            // 🎯 จำลองว่าเป็น Chrome บน Windows 10
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        var page = await browser.NewPageAsync();

        // 1.ไปที่หน้า Login
        Console.WriteLine("1. กำลังเข้าหน้า Login...");
        await page.GotoAsync("https://e-waste.diw.go.th/waste/authen/login.html");

        // 2. กรอกข้อมูลและกดปุ่มเข้าสู่ระบบ
        Console.WriteLine("2. กำลังล็อกอิน...");
        await page.Locator("#username").FillAsync("username");
        await page.Locator("#password").FillAsync("password");
        await page.Locator("#bttLogin").ClickAsync();

        // ==========================================
        // 🚨 จุดสำคัญที่สุด: ต้องสั่งให้โปรแกรม "รอ" ก่อน
        // ==========================================

        Console.WriteLine("3. รอระบบตรวจสอบรหัสผ่าน...");
        // รอจนกว่าจะเห็น "ปุ่มออกจากระบบ" หรือ "ตารางข้อมูล" ปรากฏขึ้นมา
        await page.Locator("button:has-text('Log Out')").WaitForAsync();
        Console.WriteLine("4. ล็อกอินสำเร็จ! กำลังดึงโค้ดหน้าเว็บ...");

        //เลือกบริษัท ที่ต้องการดูข้อมูล
        // 1. ค้นหาแถว (tr) ที่มีข้อความที่เราต้องการอ้างอิง
        var targetRow = page.Locator("tr").Filter(new() { HasText = "20190300225401" });

        // 2. สั่งคลิกปุ่มที่อยู่ "ภายใน" แถวนั้นเท่านั้น
        await targetRow.Locator("button:has-text('ดำเนินการ')").ClickAsync();
        Console.WriteLine("5. ดำเนินการคลิกปุ่มดำเนินการเรียบร้อยแล้ว! กำลังรอโหลดข้อมูล...");

        // เลือก 1. ยืนยันความยินยอมรับดำเนินการ สิ่งปฏิกูลหรือวัสดุที่ไม่ใช้แล้ว
        await page.Locator("a:has-text('1. ยืนยันความยินยอมรับดำเนินการ สิ่งปฏิกูลหรือวัสดุที่ไม่ใช้แล้ว')").ClickAsync();
        Console.WriteLine("6. คลิกที่ลิงก์ยืนยันความยินยอมเรียบร้อยแล้ว! กำลังรอโหลดข้อมูล...");

        // ==========================================
        // 📃  รายการคำขอรอการตอบรับ
        // ==========================================

        Console.WriteLine(" 🔃 เริ่มกระบวนการเตรียมหน้าตาราง...");
        // 1. ตั้งค่า Dropdown เป็น 100 รายการเพื่อความรวดเร็ว
        // หา <select> ที่อยู่ใกล้กับคำว่า "Show" แล้วเลือก "100"
        var lengthSelect = page.Locator("select[name='waste_pro_table1_length']");
        await lengthSelect.SelectOptionAsync(new[] { "10" });
        // รอให้ตารางรีเฟรชข้อมูล (ให้เน็ตนิ่งก่อน)
        Console.WriteLine(" 🔃 กำลังรอให้ตาราง waste_pro_table1 โหลดข้อมูลเสร็จ...");
        var firstRowOfMyTable = page.Locator("#waste_pro_table1 tbody tr").First;
        await firstRowOfMyTable.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        //เช็คจำนวนรายการ showing ว่ามีทั้งหมดกี่รายการ
        Console.WriteLine(" 🔃 กำลังตรวจสอบจำนวนรายการทั้งหมด...");
        // 1. ชี้เป้าไปที่ ID ของกล่องข้อความมุมซ้ายล่างตรงๆ
        var infoLocator = page.Locator("#waste_pro_table1_info");
        // 2. ดึงข้อความทั้งหมดออกมา (เช่น "Showing 1 to 6 of 6 entries")
        string totalInfo = await infoLocator.InnerTextAsync();
        Console.WriteLine($"ข้อความที่ดึงได้: {totalInfo}");

        var match = System.Text.RegularExpressions.Regex.Match(totalInfo, @"of\s+([0-9,]+)\s+entries");

        if (match.Success)
        {
            // ดึงตัวเลขที่อยู่ในวงเล็บของ Regex ออกมา
            string totalRecordsStr = match.Groups[1].Value.Replace(",", ""); // ลบลูกน้ำออกเผื่อมีหลักพัน
            int totalRecords = int.Parse(totalRecordsStr);

            Console.WriteLine($"=> ระบบตรวจพบข้อมูลที่ต้องดึงทั้งหมด: {totalRecords} รายการ");
        }
        else
        {
            Console.WriteLine("=> ไม่สามารถสกัดตัวเลขจำนวนรวมได้");
        }

        var exportData = new List<string>();


        // ==========================================
        // ❌❌❌❌❌❌❌ทดสอบ❌❌❌❌❌❌❌
        // ==========================================
        /*
                // 🎯 1. ดึงชื่อคอลัมน์จาก "หน้าแรกหลักสุด" มารอก่อน
                var mainThElements = await page.Locator("#waste_pro_table1 thead th").AllAsync();
                var mainHeadersList = new List<string>();
                foreach (var th in mainThElements)
                {
                    string thText = await th.InnerTextAsync();
                    if (!string.IsNullOrWhiteSpace(thText))
                    {
                        mainHeadersList.Add($"\"{thText.Trim()}\"");
                    }
                }
                string mainHeaderStr = string.Join(",", mainHeadersList);

                // ==========================================
                // 2. ดึงข้อมูลหน้าแรกหลักสุด 🎯 (จำค่าแถวเป้าหมายไว้ในกระเป๋า)
                // ==========================================
                var allMainRows = page.Locator("#waste_pro_table1 > tbody > tr");
                await allMainRows.First.WaitForAsync(new() { State = WaitForSelectorState.Visible });

                int rowCount = await allMainRows.CountAsync();
                if (rowCount < 2)
                {
                    Console.WriteLine("❌ ข้อผิดพลาด: ตารางหน้าหลักมีข้อมูลไม่ถึงแถวที่ต้องการ");
                    return;
                }

                // เลือกแถวเป้าหมาย (ตัวอย่างใช้ .Nth(7) ตามโค้ดเดิมของคุณ)
                var targetMainRow = allMainRows.Nth(9);

                var mainTds = targetMainRow.Locator("xpath=./td");
                int mainTdCount = await mainTds.CountAsync();
                var mainValuesListtest = new List<string>();

                for (int c = 0; c < mainHeadersList.Count && c < mainTdCount; c++)
                {
                    string text = await mainTds.Nth(c).InnerTextAsync();
                    mainValuesListtest.Add($"\"{text.Replace("\n", " ").Replace("\r", "").Trim()}\"");
                }
                // 🎯 นี่คือ String ข้อมูลของหน้าแรกสุดที่บอทจำไว้
                string firstPageDataStr = string.Join(",", mainValuesListtest);
                Console.WriteLine($"\n[คัดลอกข้อมูลหน้าแรกสุดสำเร็จ]: {firstPageDataStr}");

                // ==========================================
                // 3. เตรียมเปิด Modal (คลิกบรรทัดเป้าหมาย)
                // ==========================================
                Console.WriteLine("\nกำลังจะคลิกเปิด Modal...");
                await RandomDelay(1500, 3000);

                // คลิกที่คอลัมน์แรกเพื่อเปิด Modal
                await mainTds.First.ClickAsync();

                var targetModal = page.Locator(".modal-content:visible").Last;
                await targetModal.WaitForAsync();

                Console.WriteLine("Modal เปิดแล้ว กำลังเตรียมดึงข้อมูล...");
                await RandomDelay(1000, 2500);

                var innerTable = targetModal.Locator("table[id^='items_data_']").Last;

                if (await innerTable.CountAsync() > 0)
                {
                    Console.WriteLine("\nกำลังเริ่มกวาดข้อมูลแบบ ตารางซ้อนตาราง + รวมข้อมูลหน้าแรก...");

                    var allRows = await page.Locator("#CleanerDataTable > table > tbody > tr").AllAsync();
                    Console.WriteLine($"พบแถวทั้งหมดในหน้านี้ (รวมหลัก+ย่อย) = {allRows.Count} แถว\n");

                    string currentMainDataStr = ""; // จำข้อมูลหลักใน Modal
                    string innerHeaderStr = "";     // จำหัวตารางย่อยใน Modal
                    bool isFullHeaderSaved = false; // สวิตช์ล็อกสำหรับเซฟหัวตารางรวมครั้งเดียว


                    int currentButtonIndex = 1;
                    // 🎯 วนลูปอ่านทีละ 1 บรรทัดเรียงลงมาเรื่อยๆ (i++)
                    for (int i = 0; i < allRows.Count; i++)
                    {
                        var row = allRows[i];
                        var tds = row.Locator("xpath=./td");
                        int tdCount = await tds.CountAsync();

                        if (tdCount > 1)
                        {
                            // ==========================================
                            // [ใน Modal] แถวข้อมูลหลัก
                            // ==========================================
                            var mainValuesListtests = new List<string>();

                            for (int c = 0; c < mainHeadersList.Count && c < tdCount; c++)
                            {
                                string text = await tds.Nth(c).InnerTextAsync();
                                mainValuesListtests.Add($"\"{text.Replace("\n", " ").Replace("\r", "").Trim()}\"");
                            }

                            currentMainDataStr = string.Join(",", mainValuesListtests);
                        }
                        else if (tdCount == 1)
                        {
                            // ==========================================
                            // [ใน Modal] แถวซ่อนตารางย่อย
                            // ==========================================
                            var innerTables = row.Locator("table");
                            if (await innerTables.CountAsync() > 0)
                            {
                                // 🎯 ดึงหัวตารางย่อยเก็บไว้ทำ Header
                                if (string.IsNullOrEmpty(innerHeaderStr))
                                {
                                    var innerThElementsM = await innerTables.Locator("thead th").AllAsync();
                                    var innerHeadersListM = new List<string>();
                                    foreach (var th in innerThElementsM)
                                    {
                                        innerHeadersListM.Add($"\"{th.InnerTextAsync().Result.Trim()}\"");
                                    }
                                    innerHeaderStr = string.Join(",", innerHeadersListM);
                                }

                                // 🎯 4. [สร้างหัวตารางรวมเข้า CSV] เอา (หัวหน้าแรกสุด + หัวหลักใน Modal + หัวย่อยใน Modal) มาต่อกัน
                                if (!isFullHeaderSaved && !string.IsNullOrEmpty(innerHeaderStr))
                                {
                                    // คุณสามารถปรับลำดับหัวคอลัมน์ตรงนี้ได้ตามต้องการครับ
                                    string combinedHeader = "หัวหน้าแรก_" + mainHeaderStr + "," + mainHeaderStr + "," + innerHeaderStr;
                                    exportData.Insert(0, combinedHeader);
                                    isFullHeaderSaved = true;
                                    Console.WriteLine("✨ สร้างหัวข้อคอลัมน์รวมสำเร็จ: " + combinedHeader);
                                }

                                var innerDataRows = await innerTables.Locator("tbody tr").AllAsync();

                                for (int j = 0; j < innerDataRows.Count; j++)
                                {
                                    var innerTds = innerDataRows[j].Locator("xpath=./td");
                                    int innerTdCount = await innerTds.CountAsync();

                                    if (innerTdCount <= 1) continue; // ข้ามบรรทัดขยะ

                                    var innerValuesList = new List<string>();
                                    for (int c = 0; c < innerTdCount; c++)
                                    {
                                        string text = await innerTds.Nth(c).InnerTextAsync();
                                        innerValuesList.Add($"\"{text.Replace("\n", " ").Replace("\r", "").Trim()}\"");
                                    }

                                    string innerDataStr = string.Join(",", innerValuesList);

                                    // ====================================================================
                                    // 🚀 [จุดแก้ไขวิกฤต] เปลี่ยนวิธีหาปุ่มตรวจสอบ โดยค้นหาตรง ๆ จากภายใน targetModal หลัก
                                    // ====================================================================

                                    int buttonIndex = currentButtonIndex;


                                    // 🎯 เปลี่ยนจุดนี้: ค้นหาปุ่มโดยอิงจากโครงสร้างหน้าต่างหลัก (targetModal) ไม่ฟิกซ์ติดกับแถว j อีกต่อไป
                                    var inspectBtn = targetModal.Locator($"button#btt_rd{buttonIndex}").First;

                                    Console.WriteLine($"\n[แถวย่อยที่ {j + 1}] กำลังตรวจสอบปุ่ม 'ตรวจสอบ' ที่ชื่อ btt_rd{buttonIndex}...");

                                    if (await inspectBtn.IsVisibleAsync())
                                    {
                                        // 1. เลื่อนหน้าจอหลักใน Modal ลงมาโฟกัสที่ปุ่มปัจจุบันให้เห็นเด่นชัด
                                        await inspectBtn.ScrollIntoViewIfNeededAsync();

                                        Console.WriteLine($"\n[แถวย่อยที่ {j + 1}] 🔘 เจอเป้าหมายปุ่ม btt_rd{buttonIndex}! กำลังคลิกตรวจสอบ...");
                                        await inspectBtn.ClickAsync(new() { Force = true });

                                        Console.WriteLine("      ⏳ [ระบบโหลดช้า] บอทเริ่มมาตรการล็อกล้อ ดักรอหน้าต่างรายละเอียดลึกสุด...");
                                        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                                        // 2. ดักจับหน้าต่าง Modal ตัวในสุดที่ซ้อนเปิดขึ้นมาใหม่
                                        var detailModal = page.Locator(".modal-content:visible").Last;

                                        // รอจนกล่องข้อความตัวแรกสุดใน Modal เรนเดอร์ขึ้นจอจริง
                                        var firstInputInModal = detailModal.Locator("input").First;
                                        await firstInputInModal.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 20000 });

                                        Console.WriteLine("      ✨ [ความสำเร็จ] หน้าต่างกางเต็มจอแล้ว! กำลังกวาดข้อมูลหลักและอัปเดตหัวคอลัมน์...");
                                        Console.WriteLine("============ [🕵️ บอทชำแหละดึงข้อมูลคู่แบบละเอียด] ============");

                                        var detailHeadersList = new List<string>();
                                        var detailValuesList = new List<string>();

                                        // วิ่งจับแท็ก <label> ด้านบนทั้งหมดเพื่อทำชื่อคอลัมน์
                                        var labels = await detailModal.Locator("label").AllAsync();

                                        foreach (var lbl in labels)
                                        {
                                            string headerText = await lbl.InnerTextAsync();
                                            headerText = headerText.Replace("\n", " ").Replace("\r", "").Trim();

                                            if (string.IsNullOrWhiteSpace(headerText) || headerText.Contains("เอกสารประกอบ")) continue;

                                            var input = lbl.Locator("xpath=..//input | ..//textarea | ..//select").First;

                                            if (await input.CountAsync() > 0)
                                            {
                                                var isInTable = input.Locator("xpath=ancestor::table[@id='ProduceTable']");
                                                if (await isInTable.CountAsync() > 0) continue;

                                                string val = await input.InputValueAsync();
                                                val = val.Replace("\n", " ").Replace("\r", "").Trim();

                                                detailHeadersList.Add($"\"{headerText}\"");
                                                detailValuesList.Add($"\"{val}\"");

                                                Console.WriteLine($"   📍 [Column: {headerText}] ➡️ [Data: {val}]");
                                            }
                                        }

                                        string detailHeaderStr = string.Join(",", detailHeadersList);
                                        string detailDataStr = string.Join(",", detailValuesList);

                                        // บังคับสร้างและอัปเดตบรรทัดหัวตาราง (Index 0) เสมอ
                                        string completeHeader = "หัวหน้าแรก_" + mainHeaderStr + "," + mainHeaderStr + "," + innerHeaderStr + "," + detailHeaderStr;

                                        if (exportData.Count == 0)
                                        {
                                            exportData.Add(completeHeader);
                                        }
                                        else
                                        {
                                            if (!exportData[0].Contains(detailHeaderStr))
                                            {
                                                if (exportData[0].Contains(mainHeaderStr) && !exportData[0].Contains(detailHeaderStr))
                                                {
                                                    exportData.RemoveAt(0);
                                                }
                                                exportData.Insert(0, completeHeader);
                                            }
                                        }

                                        // ประกอบร่างมัดรวมข้อมูลทุกส่วนเข้าด้วยกัน
                                        string combinedRow = firstPageDataStr + "," + currentMainDataStr + "," + innerDataStr + "," + detailDataStr;
                                        exportData.Add(combinedRow);

                                        Console.WriteLine("=======================================================================");
                                        Console.WriteLine("🚀 [ประกอบร่างยัดลงไฟล์ CSV บรรทัดนี้สำเร็จ!]:");
                                        Console.WriteLine(combinedRow);
                                        Console.WriteLine("=======================================================================");

                                        // ====================================================================
                                        // 3. จังหวะสั่งปิดหน้าต่างปัจจุบัน เพื่อส่งคิวให้รอบถัดไป
                                        // ====================================================================
                                        Console.WriteLine("   --> ⏳ กวาดข้อมูลเสร็จแล้ว หน่วงเวลาแป๊บนึงก่อนสั่งปิด...");
                                        await RandomDelay(1000, 2000);

                                        // ล็อกเป้าปุ่มปิดด้วยคลาสที่แกะจาก HTML ล่าสุดของคุณ
                                        var closeBtn = detailModal.Locator("button[data-bs-dismiss='modal'].btn-secondary, button:has-text('ปิด')").First;

                                        if (await closeBtn.IsVisibleAsync())
                                        {
                                            await closeBtn.ClickAsync(new() { Force = true });
                                            Console.WriteLine("   --> 🔘 กดปุ่ม 'ปิด' หน้าต่างรายละเอียดเรียบร้อย");
                                        }
                                        else
                                        {
                                            Console.WriteLine("   --> ⚠️ หาปุ่มปิดไม่เจอ กำลังใช้แผนสำรองกดกากบาทด้านบน...");
                                            var closeIcon = detailModal.Locator("button.btn-close").First;
                                            await closeIcon.ClickAsync(new() { Force = true });
                                        }

                                        // เคลียร์แบล็คดรอปเงาสีเทาแบบรวดเร็ว
                                        var backdrop = page.Locator(".modal-backdrop");
                                        int backdropCount = await backdrop.CountAsync();
                                        for (int b = 0; b < backdropCount; b++)
                                        {
                                            try { await backdrop.Nth(b).WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 500 }); } catch { }
                                        }

                                        // ====================================================================
                                        // 4. เรดาร์นำทางล่วงหน้า: ตรวจสอบและดักรอให้ปุ่มถัดไปพร้อมทำงานจริงบนหน้าจอหลัก
                                        // ====================================================================
                                        int nextJ = j + 1;
                                        if (nextJ < innerDataRows.Count)
                                        {
                                            int nextButtonIndex = nextJ + 1;
                                            var nextInspectBtn = targetModal.Locator($"button#btt_rd{nextButtonIndex}").First;

                                            Console.WriteLine($"   --> 📡 เรดาร์กำลังเฝ้ารอปุ่ม btt_rd{nextButtonIndex} ของรอบถัดไปให้สว่างขึ้นบนจอ...");
                                            try
                                            {
                                                await nextInspectBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 7000 });
                                            }
                                            catch (TimeoutException)
                                            {
                                                Console.WriteLine($"   --> ⚠️ แอนิเมชันคืนค่าช้า บังคับหยุดพักระบบ 2 วินาทีเพื่อให้เลเยอร์หลักเซ็ตตัว...");
                                                await page.WaitForTimeoutAsync(2000);
                                            }
                                        }

                                        Console.WriteLine($"   ✨ [จบงานแถวย่อยที่ {j + 1}] ระบบส่งคิวเข้าสู่รอบถัดไปชัวร์ 100%");
                                        await page.WaitForTimeoutAsync(1000);

                                        currentButtonIndex++;
                                    }
                                    else
                                    {
                                        // กรณีไม่มีปุ่ม ให้บันทึกข้อมูลหลักคู่ข้อมูลย่อยเก็บรอก่อน
                                        string combinedRow = firstPageDataStr + "," + currentMainDataStr + "," + innerDataStr;
                                        exportData.Add(combinedRow);
                                        Console.WriteLine($"[ดึงสำเร็จ - แถวนี้ไม่มีปุ่มตรวจสอบจริง ๆ] {combinedRow}");
                                    }

                                }
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("ไม่พบตารางใน Modal นี้");
                }

                // ==========================================
                // บันทึกไฟล์ .csv
                // ==========================================
                string filePath = "AllItemsExport.csv";
                var utf8WithBom = new UTF8Encoding(true);
                await File.WriteAllLinesAsync(filePath, exportData, utf8WithBom);

                Console.WriteLine($"\n✅ กวาดข้อมูลเสร็จสิ้น! บันทึกไฟล์ '{filePath}' เรียบร้อย (รวมข้อมูลหน้าแรกแล้ว)");
        */
        // 🎯 1. ดึงชื่อคอลัมน์จาก "หน้าแรกหลักสุด" มารอก่อน (เหมือนเดิม)
        var mainThElements = await page.Locator("#waste_pro_table1 thead th").AllAsync();
        var mainHeadersList = new List<string>();
        foreach (var th in mainThElements)
        {
            string thText = await th.InnerTextAsync();
            if (!string.IsNullOrWhiteSpace(thText))
            {
                mainHeadersList.Add($"\"{thText.Trim()}\"");
            }
        }
        string mainHeaderStr = string.Join(",", mainHeadersList);

        // ==========================================
        // 2. ดึงข้อมูลหน้าแรกหลักสุด 🎯 (เปลี่ยนเป็นครอบลูปวนทุกแถว)
        // ==========================================
        var allMainRows = page.Locator("#waste_pro_table1 > tbody > tr");
        await allMainRows.First.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        int rowCount = await allMainRows.CountAsync();
        Console.WriteLine($"[ระบบเรดาร์] พบแถวในตารางหน้าแรกสุดทั้งหมด {rowCount} แถว กำลังจะไล่ทำทีละแถว...");

        // 🎯 เพิ่มลูปตรงนี้ เพื่อให้ทำงานตั้งแต่แถวแรกสุด (Index 0) ไปจนถึงแถวสุดท้าย
        for (int mainRowIdx = 0; mainRowIdx < rowCount; mainRowIdx++)
        {
            Console.WriteLine($"\n==========================================================");
            Console.WriteLine($"▶️ [เริ่มแถวหลักหน้าแรกที่ {mainRowIdx + 1} / {rowCount}]");
            Console.WriteLine($"==========================================================");

            var targetMainRow = allMainRows.Nth(mainRowIdx); // 🎯 เปลี่ยนจาก .Nth(9) มาเป็นใช้ตัวแปรลูป

            var mainTds = targetMainRow.Locator("xpath=./td");
            int mainTdCount = await mainTds.CountAsync();

            // ข้ามกรณีเจอแถวที่ไม่มีข้อมูล หรือเป็นแถวว่างระบบ
            if (mainTdCount <= 1)
            {
                Console.WriteLine($"⚠️ แถวที่ {mainRowIdx + 1} ไม่ใช่แถวข้อมูล (ข้าม...)");
                continue;
            }

            var mainValuesListtest = new List<string>();
            for (int c = 0; c < mainHeadersList.Count && c < mainTdCount; c++)
            {
                string text = await mainTds.Nth(c).InnerTextAsync();
                mainValuesListtest.Add($"\"{text.Replace("\n", " ").Replace("\r", "").Trim()}\"");
            }

            string firstPageDataStr = string.Join(",", mainValuesListtest);
            Console.WriteLine($"[คัดลอกข้อมูลหน้าแรกสุดสำเร็จ]: {firstPageDataStr}");

            // ==========================================
            // 3. เตรียมเปิด Modal (คลิกบรรทัดเป้าหมายตามรอบลูป)
            // ==========================================
            Console.WriteLine($"กำลังเปิด Modal ของแถวที่ {mainRowIdx + 1}...");
            await RandomDelay(1500, 3000);

            // คลิกที่คอลัมน์แรกเพื่อเปิด Modal
            await mainTds.First.ClickAsync();

            var targetModal = page.Locator(".modal-content:visible").Last;
            try
            {
                // ตั้ง Timeout ไว้เพื่อป้องกันบอทค้างถ้าบางแถวกดแล้ว Modal ไม่ขึ้น
                await targetModal.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
            }
            catch (TimeoutException)
            {
                Console.WriteLine($"❌ ข้อผิดพลาด: ไม่สามารถเปิด Modal ของแถวที่ {mainRowIdx + 1} ได้ในเวลาที่กำหนด (ข้ามแถวนี้)");
                continue;
            }

            Console.WriteLine("Modal เปิดแล้ว กำลังเตรียมดึงข้อมูล...");
            await RandomDelay(1000, 2500);

            var innerTable = targetModal.Locator("table[id^='items_data_']").Last;

            if (await innerTable.CountAsync() > 0)
            {
                Console.WriteLine("\nกำลังเริ่มกวาดข้อมูลแบบ ตารางซ้อนตาราง + รวมข้อมูลหน้าแรก...");

                var allRows = await page.Locator("#CleanerDataTable > table > tbody > tr").AllAsync();
                Console.WriteLine($"พบแถวทั้งหมดในหน้านี้ (รวมหลัก+ย่อย) = {allRows.Count} แถว\n");

                string currentMainDataStr = "";
                string innerHeaderStr = "";
                bool isFullHeaderSaved = false;

                int currentButtonIndex = 1;

                for (int i = 0; i < allRows.Count; i++)
                {
                    var row = allRows[i];
                    var tds = row.Locator("xpath=./td");
                    int tdCount = await tds.CountAsync();

                    if (tdCount > 1)
                    {
                        var mainValuesListtests = new List<string>();
                        for (int c = 0; c < mainHeadersList.Count && c < tdCount; c++)
                        {
                            string text = await tds.Nth(c).InnerTextAsync();
                            mainValuesListtests.Add($"\"{text.Replace("\n", " ").Replace("\r", "").Trim()}\"");
                        }
                        currentMainDataStr = string.Join(",", mainValuesListtests);
                    }
                    else if (tdCount == 1)
                    {
                        var innerTables = row.Locator("table");
                        if (await innerTables.CountAsync() > 0)
                        {
                            if (string.IsNullOrEmpty(innerHeaderStr))
                            {
                                var innerThElementsM = await innerTables.Locator("thead th").AllAsync();
                                var innerHeadersListM = new List<string>();
                                foreach (var th in innerThElementsM)
                                {
                                    innerHeadersListM.Add($"\"{th.InnerTextAsync().Result.Trim()}\"");
                                }
                                innerHeaderStr = string.Join(",", innerHeadersListM);
                            }

                            if (!isFullHeaderSaved && !string.IsNullOrEmpty(innerHeaderStr))
                            {
                                string combinedHeader = "หัวหน้าแรก_" + mainHeaderStr + "," + mainHeaderStr + "," + innerHeaderStr;
                                // 🎯 เช็กให้ชัวร์ว่าไม่แอดหัวตารางซ้ำๆ ทุกรอบหลัก
                                if (exportData.Count == 0)
                                {
                                    exportData.Insert(0, combinedHeader);
                                }
                                isFullHeaderSaved = true;
                            }

                            var innerDataRows = await innerTables.Locator("tbody tr").AllAsync();

                            for (int j = 0; j < innerDataRows.Count; j++)
                            {
                                var innerTds = innerDataRows[j].Locator("xpath=./td");
                                int innerTdCount = await innerTds.CountAsync();

                                if (innerTdCount <= 1) continue;

                                var innerValuesList = new List<string>();
                                for (int c = 0; c < innerTdCount; c++)
                                {
                                    string text = await innerTds.Nth(c).InnerTextAsync();
                                    innerValuesList.Add($"\"{text.Replace("\n", " ").Replace("\r", "").Trim()}\"");
                                }

                                string innerDataStr = string.Join(",", innerValuesList);
                                int buttonIndex = currentButtonIndex;

                                var inspectBtn = targetModal.Locator($"button#btt_rd{buttonIndex}").First;
                                Console.WriteLine($"\n[แถวย่อยที่ {j + 1}] กำลังตรวจสอบปุ่ม 'ตรวจสอบ' ที่ชื่อ btt_rd{buttonIndex}...");

                                if (await inspectBtn.IsVisibleAsync())
                                {
                                    await inspectBtn.ScrollIntoViewIfNeededAsync();
                                    Console.WriteLine($"\n[แถวย่อยที่ {j + 1}] 🔘 เจอเป้าหมายปุ่ม btt_rd{buttonIndex}! กำลังคลิกตรวจสอบ...");
                                    await inspectBtn.ClickAsync(new() { Force = true });

                                    Console.WriteLine("      ⏳ [ระบบโหลดช้า] บอทเริ่มมาตรการล็อกล้อ ดักรอหน้าต่างรายละเอียดลึกสุด...");
                                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                                    var detailModal = page.Locator(".modal-content:visible").Last;
                                    var firstInputInModal = detailModal.Locator("input").First;
                                    await firstInputInModal.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 20000 });

                                    Console.WriteLine("      ✨ [ความสำเร็จ] หน้าต่างกางเต็มจอแล้ว! กำลังกวาดข้อมูลหลักและอัปเดตหัวคอลัมน์...");

                                    var detailHeadersList = new List<string>();
                                    var detailValuesList = new List<string>();
                                    var labels = await detailModal.Locator("label").AllAsync();

                                    foreach (var lbl in labels)
                                    {
                                        string headerText = await lbl.InnerTextAsync();
                                        headerText = headerText.Replace("\n", " ").Replace("\r", "").Trim();

                                        if (string.IsNullOrWhiteSpace(headerText) || headerText.Contains("เอกสารประกอบ")) continue;

                                        var input = lbl.Locator("xpath=..//input | ..//textarea | ..//select").First;
                                        if (await input.CountAsync() > 0)
                                        {
                                            var isInTable = input.Locator("xpath=ancestor::table[@id='ProduceTable']");
                                            if (await isInTable.CountAsync() > 0) continue;

                                            string val = await input.InputValueAsync();
                                            val = val.Replace("\n", " ").Replace("\r", "").Trim();

                                            detailHeadersList.Add($"\"{headerText}\"");
                                            detailValuesList.Add($"\"{val}\"");
                                        }
                                    }

                                    string detailHeaderStr = string.Join(",", detailHeadersList);
                                    string detailDataStr = string.Join(",", detailValuesList);

                                    string completeHeader = "หัวหน้าแรก_" + mainHeaderStr + "," + mainHeaderStr + "," + innerHeaderStr + "," + detailHeaderStr;

                                    if (exportData.Count == 0)
                                    {
                                        exportData.Add(completeHeader);
                                    }
                                    else
                                    {
                                        if (!exportData[0].Contains(detailHeaderStr))
                                        {
                                            if (exportData[0].Contains(mainHeaderStr) && !exportData[0].Contains(detailHeaderStr))
                                            {
                                                exportData.RemoveAt(0);
                                            }
                                            exportData.Insert(0, completeHeader);
                                        }
                                    }

                                    string combinedRow = firstPageDataStr + "," + currentMainDataStr + "," + innerDataStr + "," + detailDataStr;
                                    exportData.Add(combinedRow);

                                    Console.WriteLine("=======================================================================");
                                    Console.WriteLine(combinedRow);
                                    Console.WriteLine("=======================================================================");

                                    Console.WriteLine("   --> ⏳ กวาดข้อมูลเสร็จแล้ว หน่วงเวลาแป๊บนึงก่อนสั่งปิด...");
                                    await RandomDelay(1000, 2000);

                                    var closeBtn = detailModal.Locator("button[data-bs-dismiss='modal'].btn-secondary, button:has-text('ปิด')").First;

                                    if (await closeBtn.IsVisibleAsync())
                                    {
                                        await closeBtn.ClickAsync(new() { Force = true });
                                        Console.WriteLine("   --> 🔘 กดปุ่ม 'ปิด' หน้าต่างรายละเอียดเรียบร้อย");
                                    }
                                    else
                                    {
                                        Console.WriteLine("   --> ⚠️ หาปุ่มปิดไม่เจอ กำลังใช้แผนสำรองกดกากบาทด้านบน...");
                                        var closeIcon = detailModal.Locator("button.btn-close").First;
                                        await closeIcon.ClickAsync(new() { Force = true });
                                    }

                                    var backdrop = page.Locator(".modal-backdrop");
                                    int backdropCount = await backdrop.CountAsync();
                                    for (int b = 0; b < backdropCount; b++)
                                    {
                                        try { await backdrop.Nth(b).WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 500 }); } catch { }
                                    }

                                    int nextJ = j + 1;
                                    if (nextJ < innerDataRows.Count)
                                    {
                                        int nextButtonIndex = nextJ + 1;
                                        var nextInspectBtn = targetModal.Locator($"button#btt_rd{nextButtonIndex}").First;

                                        Console.WriteLine($"   --> 📡 เรดาร์กำลังเฝ้ารอปุ่ม btt_rd{nextButtonIndex} ของรอบถัดไปให้สว่างขึ้นบนจอ...");
                                        try { await nextInspectBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 7000 }); }
                                        catch (TimeoutException) { await page.WaitForTimeoutAsync(2000); }
                                    }

                                    currentButtonIndex++;
                                }
                                else
                                {
                                    string combinedRow = firstPageDataStr + "," + currentMainDataStr + "," + innerDataStr;
                                    exportData.Add(combinedRow);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("ไม่พบตารางใน Modal นี้");
            }

            // 🎯 5. [จุดสำคัญมาก] หลังจากกวาดข้อมูลใน Modal ของแถวนั้นครบแล้ว ต้อง "ปิด Modal ตัวนอกสุด" เพื่อให้บอทกลับไปอยู่หน้าแรก
            Console.WriteLine($"\n[เสร็จสิ้นแถวหลักที่ {mainRowIdx + 1}] กำลังปิด Modal ชั้นนอกสุดเพื่อกลับสู่หน้าแรก...");

            // หาปุ่มปิดของ Modal หลัก (มักจะเป็นปุ่มปิดตัวที่เห็นอยู่บนจอของหน้าต่างนั้น)
            var outerCloseBtn = targetModal.Locator("button[data-bs-dismiss='modal'], button:has-text('ปิด')").Last;
            if (await outerCloseBtn.IsVisibleAsync())
            {
                await outerCloseBtn.ClickAsync(new() { Force = true });
                await page.WaitForTimeoutAsync(1500); // รอหน้าจอหน้าแรกเซ็ตตัว 1.5 วินาที
            }
        } // 🎯 จบลูปของหน้าแรกสุด (mainRowIdx)

        // ==========================================
        // บันทึกไฟล์ .csv (เลื่อนเอามาไว้นอกลูปใหญ่ เพื่อเซฟทีเดียวตอนจบงานทั้งหมด)
        // ==========================================
        string filePath = "AllItemsExport.csv";
        var utf8WithBom = new UTF8Encoding(true);
        await File.WriteAllLinesAsync(filePath, exportData, utf8WithBom);

        Console.WriteLine($"\n✅ ดำเนินการกวาดข้อมูลครบทุกแถวสำเร็จเรียบร้อย! บันทึกไฟล์ '{filePath}' แล้ว");



        // ==========================================
        // ❌❌❌❌❌❌จบการทดสอบ❌❌❌❌❌       
        // ==========================================
        await page.PauseAsync();


    }
}
