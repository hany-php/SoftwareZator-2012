# تقرير إصلاح SoftwareZator 2012

> [!NOTE]
> هذا التقرير يوثق جميع المشاكل التي واجهها المشروع والحلول التي تم تطبيقها.
> **تاريخ التقرير:** 21 يناير 2026

---

## ملخص تنفيذي

تم إصلاح تطبيق **SoftwareZator 2012** الذي كان ينهار عند محاولة إنشاء مشروع جديد. المشكلة كانت مرتبطة بـ **Workflow Designer** ومكونات **Windows Communication Foundation (WCF)**.

---

## المشكلة الأولى: FailFast Crash

### الوصف
كان التطبيق ينهار بشكل كامل مع خطأ `System.Environment.FailFast` غير قابل للاسترداد.

### رسالة الخطأ
```
An unrecoverable error occurred. For diagnostic purposes, this English message is associated with the failure: 
'OnAssemblyLoad should not throw exception'.
```

### Stack Trace
```
at System.Environment.FailFast(System.String)
at System.Runtime.Fx.AssertAndFailFast(System.String)
at System.Activities.Core.Presentation.RegisterMetadataDelayedWorker.OnAssemblyLoaded(System.Object, System.AssemblyLoadEventArgs)
```

### السبب الجذري
الـ `RegisterMetadataDelayedWorker` في `System.Activities.Core.Presentation` يقوم بتسجيل event handler على `AppDomain.CurrentDomain.AssemblyLoad`. عند حدوث خطأ داخلي أثناء تحميل التجميعات، يتم استدعاء `FailFast` الذي يُغلق التطبيق فوراً.

### الحل المُطبق
تم تعديل ملف `ApplicationEvents.vb` لإزالة الـ event handler قبل أن يتسبب في المشكلة:

**الملف:** `SoftwareZator 2012 E.P\ApplicationEvents.vb`

```vb
Private Sub PreLoadWorkflowDesignerAssemblies()
    Try
        ' Load the required assemblies
        Dim activitiesPresentationAsm = Assembly.Load("System.Activities.Presentation, ...")
        Dim coreActivitiesPresentationAsm = Assembly.Load("System.Activities.Core.Presentation, ...")
        
        ' Get the RegisterMetadataDelayedWorker type
        Dim delayedWorkerType = coreActivitiesPresentationAsm.GetType("System.Activities.Core.Presentation.RegisterMetadataDelayedWorker")
        
        ' Get the singleton instance
        Dim instanceField = delayedWorkerType.GetField("instance", BindingFlags.Static Or BindingFlags.NonPublic)
        Dim instance = instanceField.GetValue(Nothing)
        
        ' Get the OnAssemblyLoaded event handler
        Dim handlerField = delayedWorkerType.GetField("onAssemblyLoadHandler", BindingFlags.Instance Or BindingFlags.NonPublic)
        Dim handler = TryCast(handlerField.GetValue(instance), AssemblyLoadEventHandler)
        
        ' Remove the event handler from AssemblyLoad
        If handler IsNot Nothing Then
            RemoveHandler AppDomain.CurrentDomain.AssemblyLoad, handler
        End If
        
        ' Set _initialized to True
        Dim initializedField = delayedWorkerType.GetField("_initialized", BindingFlags.Static Or BindingFlags.NonPublic)
        initializedField.SetValue(Nothing, True)
        
    Catch ex As Exception
        ' Silent catch - if we can't do the workaround, let it proceed normally
    End Try
End Sub
```

---

## المشكلة الثانية: TypeLoadException

### الوصف
بعد حل المشكلة الأولى، ظهر خطأ جديد عند محاولة إنشاء مشروع.

### رسالة الخطأ
```
System.TypeLoadException
Could not load type 'System.ServiceModel.MessageQuerySet' from assembly 
'System.ServiceModel.Activities, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'.
```

### السبب الجذري
مكونات **Windows Communication Foundation (WCF)** غير مُفعّلة على نظام Windows 11.

### الحل المُطبق
تم تفعيل مكونات WCF باستخدام أوامر DISM:

```powershell
# تشغيل كـ Administrator
DISM /Online /Enable-Feature /FeatureName:WCF-HTTP-Activation45 /All
DISM /Online /Enable-Feature /FeatureName:WCF-TCP-Activation45 /All
DISM /Online /Enable-Feature /FeatureName:WCF-Pipe-Activation45 /All
DISM /Online /Enable-Feature /FeatureName:WCF-MSMQ-Activation45 /All
```

### الطريقة البديلة (واجهة رسومية)
1. افتح **Control Panel** ← **Programs** ← **Turn Windows features on or off**
2. ابحث عن **.NET Framework 4.8 Advanced Services**
3. وسّعها وفعّل **WCF Services** وجميع مكوناتها الفرعية
4. اضغط OK وأعد تشغيل الكمبيوتر

---

## المشكلة الثالثة: خطأ تحويل التاريخ

### الوصف
كان التطبيق يظهر خطأ عند تحليل تاريخ انتهاء صلاحية الترخيص.

### رسالة الخطأ
```
String '----' was not recognized as a valid DateTime.
```

### الملف المتأثر
`SoftwareZator 2012 E.P\Presentation\SZ.Main.vb`

### الحل المُطبق
تم تعديل الكود ليتحقق من صحة التاريخ قبل محاولة تحويله:

```vb
' Before (الكود القديم)
If Date.Parse(DLL.Mes.ExpirationDate) < Date.Now Then

' After (الكود الجديد)
Dim expirationDate As Date
If Date.TryParse(DLL.Mes.ExpirationDate, expirationDate) AndAlso expirationDate < Date.Now Then
```

---

## ملخص الملفات المُعدّلة

| الملف | نوع التعديل | الوصف |
|-------|-------------|-------|
| `ApplicationEvents.vb` | إضافة كود | Reflection workaround لإزالة event handler |
| `SZ.Main.vb` | تعديل كود | إصلاح تحويل التاريخ |

---

## متطلبات النظام

لتشغيل المشروع بشكل صحيح، تأكد من توفر:

- [x] **.NET Framework 4.0** أو أحدث
- [x] **Visual Studio 2010/2012** أو أحدث
- [x] **WCF Services** مُفعّلة في Windows Features
- [x] **Workflow Foundation** مُثبت

---

## خطوات إعادة البناء

```powershell
# 1. افتح Visual Studio كـ Administrator
# 2. افتح Solution: SoftwareZator-2012.sln
# 3. Clean Solution
# 4. Rebuild Solution
# 5. Start Debugging (F5)
```

---

## ملاحظات إضافية

> [!WARNING]
> **هام:** إذا واجهت مشاكل مستقبلية مع Workflow Designer، تأكد أولاً من:
> 1. أن WCF Services مُفعّلة
> 2. أن جميع التجميعات المطلوبة موجودة في GAC
> 3. أن التطبيق يعمل بصلاحيات المسؤول

---

## جهات الاتصال

للمزيد من المساعدة، راجع:
- ملف `ApplicationEvents.vb` للكود المُضاف
- هذا التقرير للرجوع السريع

---

*تم إنشاء هذا التقرير تلقائياً بتاريخ 21 يناير 2026*
