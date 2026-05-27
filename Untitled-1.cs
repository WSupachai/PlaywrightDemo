 // // 1. กวาดป้าย <label> ทั้งหมดที่อยู่ใน Modal ออกมาก่อน
        // var allLabels = await targetModal.Locator("label").AllAsync();

        // Console.WriteLine("--- ค่าทั้งหมดใน Modal ---");

        // foreach (var label in allLabels)
        // {
        //     // ดึงข้อความจากป้ายชื่อ
        //     string labelName = await label.InnerTextAsync();

        //     // 2. ใช้ xpath=.. ถอยหลังขึ้นไปหา "กล่องแม่" ที่คลุม Label นี้อยู่ (ไม่สนว่าจะเป็น col-6 หรืออะไร)
        //     var parentBox = label.Locator("xpath=..");

        //     // 3. หากล่องกรอกข้อมูล "ทุกประเภท" ที่อยู่ในกล่องแม่เดียวกัน
        //     var inputFields = parentBox.Locator("input, textarea, select");

        //     // เช็กว่ามีช่องกรอกข้อมูลอยู่คู่กับ Label นี้จริงๆ ใช่ไหม (บางที Label อาจจะใช้ทำอย่างอื่น)
        //     if (await inputFields.CountAsync() > 0)
        //     {
        //         // ดึงช่องแรกที่เจอมาอ่านค่า
        //         var targetInput = inputFields.First;

        //         string inputValue = await targetInput.InputValueAsync();

        //         // (เสริม) ถ้าช่องนั้นเป็น Dropdown <select> แล้วได้ค่าเป็นตัวเลข ID แทนที่จะเป็นตัวหนังสือ 
        //         // ให้คอมเมนต์บรรทัดบน แล้วปลดคอมเมนต์บรรทัดล่างนี้ไปใช้แทนครับ
        //         // string inputValue = await targetInput.EvaluateAsync<string>("el => el.options ? el.options[el.selectedIndex].text : el.value");

        //         Console.WriteLine($"[{labelName.Trim()}] : {inputValue}");
        //     }
        //     else
        //     {
        //         // กรณีที่หาช่อง input ไม่เจอ แต่อาจจะเป็นแค่ข้อความธรรมดาที่แปะไว้
        //         string textValue = await parentBox.InnerTextAsync();
        //         // เอาชื่อ Label ออก จะได้เหลือแต่ค่าข้อมูล
        //         textValue = textValue.Replace(labelName, "").Trim();
        //         Console.WriteLine($"[{labelName.Trim()}] : {textValue} (ข้อความธรรมดา)");
        //     }
        // }


