var Superstore = LoadSuperstore();


/**
 * Передается любое количество объектов (>=1).
 * Все свойства объектов, прсваиваются первому объекту
 * Взято отсюда: https://stackoverflow.com/a/12534361
 */
function update(obj /*, …*/ ) {
    for (var i = 1; i < arguments.length; i++) {
        for (var prop in arguments[i]) {
            if (arguments[i].hasOwnProperty(prop)) {
                var val = arguments[i][prop];
                if (typeof val === "object") // this also applies to arrays or null!
                    update(obj[prop], val);
                else
                    obj[prop] = val;
            }
        }
    }
    return obj;
}

function GetCurrentDayOfWeek() {
    var result = new Date().getDay();
    if (result === 0) {  // Воскресенье
        return 7;
    }
    return result;
}

/* Enum для понятности кода в дальнейшем */
var DaysOfWeekEnum = {
    Monday: 1,
    Tuesday: 2,
    Wednesday: 3,
    Thursday: 4,
    Friday: 5,
    Saturday: 6,
    Sunday: 7
};

/* Перевод дней недели на русский. Числа соответствуют значениям у DaysOfWeekEnum */
var DaysOfWeekTranslation = {
    1: "Понедельник",
    2: "Вторник",
    3: "Среда",
    4: "Четверг",
    5: "Пятница",
    6: "Суббота",
    7: "Воскресенье"
};
var DaysOfWeekTranslationReverse = {
    "Понедельник": 1,
    "Вторник": 2,
    "Среда": 3,
    "Четверг": 4,
    "Пятница": 5,
    "Суббота": 6,
    "Воскресенье": 7
};

/**
 * Базовый прототип для строки в расписании (один урок).
 * Все записи расписания должны от него наследоваться.
 */
var ScheduleItem = {
    No: 1, // Номер урока
    StartHours: 15, // Часы начала урока
    StartMinutes: 0, // Минуты начала урока
    EndHours: 16, // Часы конца урока
    EndMinutes: 45, // Минуты конца урока
    IsReplace: false,  // Замена ли на этом уроке

    // Данные с двух подчеркиваний, т.к. эти данные не загружены с сервера
    LessonName: "__Математика",
    TeacherName: "__Наталья Ивановна",
    Classroom: -123,

    DayOfWeek: DaysOfWeekEnum.Monday, // День недели, когда этот урок идёт. Значение должно быть из DaysOfWeekEnum
    ClassNum: -1, // Цифра класса
    ClassLetter: "__А", // Буква класса
    
    /**
     * Добивает нулями строку до двух символов. Например, 5 => 05; 10 => 10
     * @return {string}
     */
    _timeZFill: function(num) {
        return ("00" + num.toString()).slice(-2);
    },

    /**
     * Возвращает время начала урока в формате "15:00"
     * @return {string}
     */
    GetStartTime: function() {
        return this._timeZFill(this.StartHours) + ":" + this._timeZFill(this.StartMinutes);
    },

    /**
     * Возвращает время конца урока в формате "15:00"
     * @return {string}
     */
    GetEndTime: function() {
        return this._timeZFill(this.EndHours) + ":" + this._timeZFill(this.EndMinutes);
    },

    /**
     * Возвращает идёт ли сейчас урок
     * @return {boolean}
     */
    IsNow: function() {
        var current = new Date();
        var currentMinutes = current.getHours() * 60 + current.getMinutes();
        return currentMinutes >= (this.StartHours * 60 + this.StartMinutes) &&
            currentMinutes <= (this.EndHours * 60 + this.EndMinutes);
    },

    /**
     * Возвращает сколько минут до начала или конца урока (что раньше)
     * @return {int}
     */
    GetLeftTime: function() {
        var current = new Date();
        var currentMinutes = current.getHours() * 60 + current.getMinutes();
        var endMinutes = 0;
        if (this.IsNow()) {
            endMinutes = this.EndHours * 60 + this.EndMinutes;
        } else {
            endMinutes = this.StartHours * 60 + this.StartMinutes
        }
        return endMinutes - currentMinutes;
    }
};


/* Фильтр расписания */
var FilterBase = {
    /**
     * Функция, которая ищет подходящие уроки в расписании.
     * Schedule -- список объектов, наследуемых от ScheduleItem
     * Возврщает список объектов, наследуемых от ScheduleItem, у которых все параметры подходят под настройки фильтра
     */
    FindLessons: function(schedule) {
        var result = [];
        if (this.IsEnabled()) {
            for (var i = 0; i < schedule.length; i++) {
                var lesson = schedule[i];
                /**
                 * Условия:
                 * 1. Включен ли фильтр (false => результат true)
                 * 2. Null ли нужное значение (true => результат true)
                 * 3. Подходит ли значение. (true => результат true)
                 * 
                 * Таблица истинности:
                 *  1   2   3   Result  
                 * --- --- --- -------- 
                 *  ✗   ✗   ✗   ✓ 
                 *  ✓   ✗   ✗   ✗ 
                 *  ✗   ✓   ✗   ✓ 
                 *  ✗   ✗   ✓   ✓ 
                 *  ✓   ✓   ✗   ✓ 
                 *  ✓   ✗   ✓   ✓ 
                 *  ✗   ✓   ✓   ✓ 
                 *  ✓   ✓   ✓   ✓ 
                 * 
                 * Итого: !(!1 && !2 && !3)
                 */
                if (
                    (lesson.DayOfWeek.toString() === this.DayOfWeek.toString()) &&  // День недели. Всегда включен
                    !(this.NoEnabled && !!this.No && lesson.No !== this.No) &&  // Номер урока
                    !(this.ClassEnabled && !!this.ClassNum && lesson.ClassNum.toString() !== this.ClassNum) &&  // Цифра класса
                    !(this.ClassEnabled && !!this.ClassLetter && lesson.ClassLetter !== this.ClassLetter) &&  // Буква класса
                    !(this.LessonEnabled && !!this.LessonName && lesson.LessonName !== this.LessonName) &&  // Название предмета
                    !(this.TeacherEnabled && !!this.TeacherName && lesson.TeacherName !== this.TeacherName) &&  // Имя учителя
                    !(this.ClassroomEnabled && !!this.Classroom && lesson.Classroom.toString() !== this.Classroom)  // Номер кабинета
                ) {
                    result.push(lesson);
                }
            }
        }

        // Сортирует по номеру урока.
        return result.sort(function(a, b) {
            if (a.No < b.No) {
                return -1;
            }
            if (a.No > b.No) {
                return 1;
            }
            return 0;
        });
    },

    /* Загружает настройки фильтра из LocalStorage */
    LoadFromStorage: function() {
        console.log("Loading storage for '" + this._prefix + "'");
        var loaded = Superstore.local.get(this._prefix + 'Filter');
        if (loaded) { // Если не null или undefined
            update(this, JSON.parse(loaded));
        }
    },

    /* Сохраняет настройки фильтра в LocalStorage */
    UpdateStorage: function() {
        console.log("Updating storage for '" + this._prefix + "'");
        Superstore.local.set(this._prefix + 'Filter', JSON.stringify(this));
    },
    
    /* Выдаёт, включен ли фильтр */
    IsEnabled: function() {
        return  (this.NoEnabled && !!this.No) ||
                (this.ClassEnabled && !!this.ClassNum) ||
                (this.ClassEnabled && !!this.ClassLetter) ||
                (this.LessonEnabled && !!this.LessonName) ||
                (this.TeacherEnabled && !!this.TeacherName) ||
                (this.ClassroomEnabled && !!this.Classroom)
    },

    Test: function() {
        this.ClassNum = 12;
        this.ClassLetter = 'Ы';
    }
};

// Основной фильтр
var Filter = {
    _prefix: 'main',  // Префикс для сохранения и получения данных из localstorage

    NoEnabled: false, // Включен ли фильтр по номеру урока
    No: null, // Нужный номер урока

    ClassEnabled: false, // Включен ли фильтр по классу
    ClassNum: null, // Нужная цифра класса
    ClassLetter: null, // Нужная буква класса

    LessonEnabled: false, // Включен ли фильтр по предмету (пока нигде не испоьзуется)
    LessonName: null, // Нужное имя предмета

    TeacherEnabled: false, // Включен ли фильтр по учителю
    TeacherName: null, // Нужное имя учителя

    ClassroomEnabled: false, // Включен ли фильтр по кабинету
    Classroom: null, // Нужный кабинет

    DayOfWeek: DaysOfWeekEnum.Monday, // День недели. Единственный обязательный параметр
};
Filter.__proto__ = FilterBase;

// Фильтр по параметрам класса в настройках
var DefaultFilter = {
    _prefix: 'settings',  // Префикс для сохранения и получения данных из localstorage

    NoEnabled: false, // Включен ли фильтр по номеру урока
    No: null, // Нужный номер урока

    ClassEnabled: true, // Включен ли фильтр по классу
    ClassNum: null, // Нужная цифра класса
    ClassLetter: null, // Нужная буква класса

    LessonEnabled: false, // Включен ли фильтр по предмету (пока нигде не испоьзуется)
    LessonName: null, // Нужное имя предмета

    TeacherEnabled: false, // Включен ли фильтр по учителю
    TeacherName: null, // Нужное имя учителя

    ClassroomEnabled: false, // Включен ли фильтр по кабинету
    Classroom: null, // Нужный кабинет

    DayOfWeek: DaysOfWeekEnum.Monday, // День недели. Единственный обязательный параметр
};
DefaultFilter.__proto__ = FilterBase;

// Инициализация движка
var app;
function initApp() {
    app = new Vue({
        el: "#app",
        template: '#main-page',
        data: {
            // Чтобы вкладки работали
            activeIndex: 0,
            tabs: [
                {
                    page: { template: '#schedule' },
                    key: "schedulePage"
                },
                {
                    page: { template: '#filter' },
                    key: "filterPage"
                },
                {
                    page: { template: '#settings' },
                    key: "settingsPage"
                }
            ],

            // Контроль за изменением
            show: true,

            // Загружались ли фильтры
            storageLoaded: false,

            title: "Школьное расписание",
            version: "<none>",

            // Данные с двух подчеркиваний, т.к. эти данные не загружены с сервера
            classLetters: ['__А'], // Все возможные буквы классов
            classNums: [-1], // Все возможные цифры классов
            classrooms: ['__123'],

            teachers: {
                '__Математика': ['__Наталья Ивановна']
            }, // Учителя по группам. В этом случае, Наталья Ивановна ведёт математику
            teacherGroup: '__Математика', // Текущая выбранная группа учителей

            // Объект фильтра. С его параметрами связываются галочки, выпадающие списка и просто поля ввода. Также он выдает отфильтрованное расписание
            'Filter': Filter,
            'DefaultFilter': DefaultFilter,  // Фильтр для уведомлений и списка уроков, если основной не настроен
            schedule: [ScheduleItem], // Расписание на всю школу

            enableNotification: false,  // Отображать ли уведомление
            enableBackground: false,  // Работать ли в фоне

            daysOfWeekTranslationReverse: DaysOfWeekTranslationReverse,  // Для списка дней недели
            daysOfWeekTranslation: DaysOfWeekTranslation,  // Для текущего дня недели

            'help': 0  // Номер этапа помощи
        },
        created: function() {
            // Как только движок инициализировался фильтр загружает параметры из LocalStorage.
            this.Filter.LoadFromStorage();
            this.DefaultFilter.LoadFromStorage();
            this.DefaultFilter.ClassEnabled = true;  // Этот фильтр всегда включен
            this.enableNotification = Superstore.local.get('notification') === "true";
            this.enableBackground = Superstore.local.get('background') === "true";
            cordova.plugins.backgroundMode.setEnabled(this.enableNotification && this.enableBackground);
            this.storageLoaded = true;
        },
        mounted: function() {
            // Настраивает автодополнение (дожидаясь пока #classroomFilterInner появится)
            var initAutocompleteFunc = function() {
                var classroomInner = document.getElementById("classroomFilterInner");
                if (classroomInner === null) {
                    setTimeout(initAutocompleteFunc, 100);
                } else {
                    classroomInner.setAttribute("list", "classroomList");
                    //new Awesomplete(classroomInner, {list: "#classroomList", minChars: 1, maxItems: 15});
                    setTimeout(initAutocompleteFunc, 1000);  // Почему-то сбрасывется. Скорее всего onsen пересоздаёт элемент
                }
            };
            initAutocompleteFunc();

            // Отображает помощь
            if (Superstore.local.get('helpShown') !== true) {
                this.updateHelp()
            }
        },
        methods: {
            // Доступные функции для использования в шаблонах html.
            getKeys: function(obj) {
                return Object.keys(obj);
            },

            getCurrentDayOfWeek: GetCurrentDayOfWeek,

            rerender: function() {
                this.show = false;
                this.$nextTick(function() {
                    this.show = true
                });
            },

            updateData: function() {
                loadData(updateData);
            },

            findLessons: function() {
                if (this.Filter.IsEnabled()) {
                    return this.Filter.FindLessons(this.schedule);
                }
                if (this.DefaultFilter.IsEnabled()) {
                    return this.DefaultFilter.FindLessons(this.schedule);
                }
                return [];
            },

            updateHelp: function () {
                // В html номера этапов на один больше! Для понятности в case вычитается эта единица, но тогда нужно быть внимательным с номером следующего этапа
                console.log("Before: " + this.help);
                switch (this.help) {
                    case 1 - 1:
                        // Раписание (вкладка)
                        this.activeIndex = 0;
                        this.Filter.ClassNum = "5";
                        this.Filter.ClassLetter = "Б";
                        this.Filter.ClassEnabled = true;
                        this.Filter.ClassroomEnabled = false;
                        this.Filter.TeacherEnabled = false;

                        setTimeout(function() {
                            app.help = 1
                        }, 500);
                        break;
                    case 2 - 1:
                        // Фильтры (вкладка)
                        this.activeIndex = 1;
                        setTimeout(function() {
                            app.help = 2
                        }, 500);
                        break;
                    case 3 - 1:
                        // День недели (фильтр)
                        this.help = 3;
                        break;
                    case 4 - 1:
                        // Класс (фильтр)
                        this.help = 4;
                        break;
                    case 5 - 1:
                        // Учитель (фильтр)
                        this.help = 5;
                        break;
                    case 6 - 1:
                        // Кабинет (фильтр)
                        this.help = 6;
                        break;
                    case 7 - 1:
                        // Настройки (вкладка)
                        this.activeIndex = 2;
                        setTimeout(function() {
                            app.help = 7
                        }, 500);
                        break;
                    case 8 - 1:
                        // Мой класс (настройка)
                        this.help = 8;
                        break;
                    case 9 - 1:
                        // Уведомление (настройка)
                        this.help = 9;
                        break;
                    case 10 - 1:
                        // Работать в фоне (настройка)
                        if (this.isMobile()) {
                            // Если мобильное, то показать про "работать в фоне"
                            this.help = 10;
                        } else {
                            //app.help = 11;  // (спрятал скачивание apk)
                            app.help = 13;
                        }
                        break;
                    case 11 - 1:
                        // Возвращает на главную страницу
                        this.activeIndex = 0;
                        setTimeout(function() {
                            if (!app.isMobile()) {
                                // Если в браузере

                                if (ons.platform.isAndroidPhone() || ons.platform.isAndroidTablet()) {
                                    // Если на андроид
                                    //app.help = 11;  // (спрятал скачивание apk)
                                    app.help = 13;
                                } else if (ons.platform.isIPhone() || ons.platform.isIPad() || ons.platform.isIPhoneX()) {
                                    // Если на iOS
                                    app.help = 13;
                                } else {
                                    // Если просто в браузере (не мобильном)
                                    app.help = 14;
                                    app.updateHelp();
                                }
                            } else {
                                // Если уже установлен
                                app.help = 14;
                                app.updateHelp();
                            }
                        }, 500);
                        break;
                    case 12 - 1:
                        // Предлагает установить android
                        this.help = 12;
                        break;
                    case 13 - 1:
                        // Предлагает добавить значок
                        this.help = 13;
                        break;
                    case 14 - 1:
                        // После добавления значка
                        this.help = 14;
                        this.updateHelp();
                        break;
                    case 15 - 1:
                        // Сохраняет, что пользователь прошел обучение
                        this.help = -1;
                        Superstore.local.set('helpShown', true);
                        break;
                }
                console.log("After: " + this.help);
            },
            
            isIOS: function() {
                return ons.platform.getMobileOS() !== "android";
            },

            isMobile: function() {
                return cordova.platformId !== "browser";
            }
        },
        watch: {
            // Настраивает сохранение настроек и перерисовку страницы при изменении параметров фильтра
            'Filter': {
                handler: function(val, oldVal) {
                    if (this.storageLoaded) {
                        this.Filter.UpdateStorage();
                    }
                },
                deep: true,
                immediate: true
            },
            'DefaultFilter': {
                handler: function(val, oldVal) {
                    if (this.storageLoaded) {
                        this.DefaultFilter.UpdateStorage();
                        updateNotification();
                    }
                },
                deep: true,
                immediate: true
            },
            'enableNotification': {
                handler: function(val, oldVal) {
                    cordova.plugins.backgroundMode.setEnabled(this.enableNotification && this.enableBackground);
                    if (this.storageLoaded) {
                        Superstore.local.set('notification', JSON.stringify(this.enableNotification));
                    }
                    updateNotification();
                },
                immediate: true
            },
            'enableBackground': {
                handler: function(val, oldVal) {
                    cordova.plugins.backgroundMode.setEnabled(this.enableNotification && this.enableBackground);
                    if (this.storageLoaded) {
                        Superstore.local.set('background', JSON.stringify(this.enableBackground));
                    }
                    updateNotification();
                },
                immediate: true
            }
        }
    });
}

/* Загружает данные в vue. Только после его создания! */
function updateData(data) {
    if (data) {
        for (var key in data) {
            if (data.hasOwnProperty(key)) {
                app.$set(app, key, data[key]);
            }
        }
    }
    app.rerender();
}

/* Загружает и обрабатывает данные с сервера */
function loadData(handler) {
    /**
     * Скачивает, сохраняет и всёозвращает данные.
     * Если данные не удалось скачать, то возвращает данные из localStorage.
     * Если в localStorage не было данных, то возвращает null.
     */
    function downloadData(hdlr) {
        /*
         * Если что-то пошло не так и не удалось скачать данные (интернета нет, например),
         * то данные грузятся из LocalStorage, если они там есть.
         * Если их там нет, то функция возвращает null.
         */
        function loadCached() {
            var dataFromStorage = Superstore.local.get('Data');
            if (!dataFromStorage) hdlr(null);
            data = dataFromStorage;
            hdlr(data);
        }

        loadCached();  // Сначала загружает из кеша, пока грузится из интернета
        var data = '';
        // Проверка есть ли интернет
        if (navigator.onLine) {
            var xhr = new XMLHttpRequest();
            xhr.open('GET', "https://bitbucket.org/iliago/schoolschedule/raw/HEAD/scheduleData", true);
            xhr.onload = function (e) {
                if (xhr.readyState === 4) {
                    if (xhr.status === 200) {
                        Superstore.local.set('Data', xhr.response);
                        hdlr(xhr.response.toString())
                    }
                }
            };
            xhr.send(null);
        }
    }
    
    /**
     * Эффективно читает строки из data и каждую строку передаёт в func
     */
    /*function readLines(data, func) {
        var i = j = 0;
        while ((j = data.indexOf('\n', i)) !== -1) {
            var string = data.substring(i, j).trimRight("\n");
            if (string === "")
                debugger;
            func(string);
            i = j + 1;
        }
        func("");
    }*/
    function readLines(data, func) {
        var splitted = data.split('\n');
        for (i in splitted) {
            func(splitted[i].trimRight("\n"));
        }
    }
    
    /**
     * data это строка csv.
     * func это функция, которая принимает список значений и аккумулятор, возвращает новый аккумулятор.
     * accum это начальное значение аккумулятора.
     * Возвращает последний аккумулятор.
     */
    function csvParser(data, func, accum) {
        readLines(data, function(line) {
            if (line[0] === '#') return; // Эта строка -- комментарий
            if (!line) return; // Пустая строка
            var values = line.split(';');
            accum = func(values, accum);
        });
        return accum;
    }

    /*
     * Возвращает номер версии приложения и данных расписания
     */
    function parseVersion(section) {
        return csvParser(section, function(values, accum) {
            return "<b>schedule</b><br/>" +
                    "time: <code>" + values[0] + "</code><br/>" +
                    "md5: <code>" + values[1] + "</code><br/>" +
                    "sha1: <code>" + values[2] + "</code><br/>" +
                    "<b>app</b><br/>" +
                    "md5: <code>" + TOTAL_MD5 + "</code><br/>" +
                    "sha1: <code>" + TOTAL_SHA1 + "</code><br/>" +
                    "blake: <code>" + TOTAL_BLAKE + "</code>"
        }, null);
    }

    /**
     * section это строка с учителями.
     * Возвращает словарь {id: имя}.  Id это строка
     */
    function parseTeachers(section) {
        return csvParser(section, function(values, accum) {
            accum[values[0]] = values[1];
            return accum;
        }, {});
    }
    
    /**
     * section это строка с предметами.
     * Возвращает словарь {id: имя}. Id это строка
     */
    function parseSubjects(section) {
        return csvParser(section, function(values, accum) {
            accum[values[0]] = values[1];
            return accum;
        }, {});
    }
    
    /**
     * section это строка со cвязью учителей и предметов.
     * teachers и subjects это словари {id: имя} учителей и предметов, соответсвенно
     * Возвращает словарь {имя предмета: [имена учителей...]}.
     */
    function parseGroups(section, teachers, subjects) {
        var ids = csvParser(section, function(values, accum) {
            var subjId = parseInt(values[0]);
            if (!(subjId in accum)) {
                accum[subjId] = [];
            }
            for (var i = 1; i < values.length; i++) {
                var teacherId = parseInt(values[i]);
                accum[subjId].push(teachers[teacherId])
            }
            return accum;
        }, {});
        var result = {};
        for (var subjId in ids) {
            result[subjects[subjId]] = ids[subjId];
        }
        return result;
    }

    /**
     * section это строка с расписанием звонков
     * Возвращает словарь {номер урока: {
     *   start: {hours: x, minutes: x},
     *   end: {hours: x, minutes: x}
     * }...}
     */
    function parseTimetable(section) {
        return csvParser(section, function(values, accum) {
            accum[parseInt(values[0])] = {
                start: {
                    hours: parseInt(values[1]),
                    minutes: parseInt(values[2])
                },
                end: {
                    hours: parseInt(values[3]),
                    minutes: parseInt(values[4])
                }
            };
            return accum;
        }, {});
    }
    
    /**
     * section это строка с расписанием.
     * teachers и subjects это словари {id: имя} учителей и предметов, соответсвенно
     * timetable это словарь с расписанием звонков. Подробнее в parseTimetable()
     * Возвращает словарь {
     *     schedule: [ScheduleItem...],
     *     letters: [буквы классов...],
     *     numbers: [цифры классов...]
     * }.
     */
    function parseSchedule(section, teachers, subjects, timetable) {
        return csvParser(section, function(values, accum) {
            var time = timetable[parseInt(values[1])]
            var schedItem = {
                DayOfWeek: parseInt(values[0]),
                No: parseInt(values[1]),
                ClassNum: parseInt(values[2]),
                ClassLetter: values[3],
                TeacherName: teachers[values[4]],
                LessonName: subjects[values[5]],
                Classroom: values[6],
                StartHours: time.start.hours,
                StartMinutes: time.start.minutes,
                EndHours: time.end.hours,
                EndMinutes: time.end.minutes
            };
            
            if (accum.letters.indexOf(schedItem.ClassLetter) == -1) {
                accum.letters.push(schedItem.ClassLetter);
            }
            if (accum.numbers.indexOf(schedItem.ClassNum) == -1) {
                accum.numbers.push(schedItem.ClassNum);
            }
            if (accum.classrooms.indexOf(schedItem.Classroom) == -1) {
                accum.classrooms.push(schedItem.Classroom);
            }
            
            schedItem.__proto__ = ScheduleItem;
            accum.schedule.push(schedItem);
            
            return accum;
        }, {
            schedule: [],
            letters: [],
            numbers: [],
            classrooms: []
        });
    }
    
    // Получаю данные
    var data = downloadData(function(data){
        handler((function() {
            if (!data) {
                return null;
            }

            var version = null;
            var classrooms = null;
            var timetable = null;
            var letters = null;
            var numbers = null;
            var teachers = null;
            var subjects = null;
            var groups = null;
            var schedule = null;
            
            var regexp = /\[(.*)\]/;
            var sectionName = "";
            var sectionData = "";
            
            readLines(data, function(line) {
                if (line === "") {
                    switch (sectionName) {
                        case "version":
                            version = parseVersion(sectionData);
                        case "teachers":
                            teachers = parseTeachers(sectionData)
                            break;
                        case "subjects":
                            subjects = parseSubjects(sectionData);
                            break;
                        case "groups":
                            if (teachers === null || subjects === null) {
                                console.log('Data order error!');
                                return null;
                            }
                            groups = parseGroups(sectionData, teachers, subjects)
                            break;
                        case "timetable":
                            timetable = parseTimetable(sectionData, teachers, subjects);
                            break;
                        case "schedule":
                            if (teachers === null || subjects === null || timetable === null) {
                                console.log('Data order error!');
                                return null;
                            }
                            var schedResult = parseSchedule(sectionData, teachers, subjects, timetable);
                            schedule = schedResult.schedule;
                            letters = schedResult.letters;
                            numbers = schedResult.numbers;
                            classrooms = schedResult.classrooms;
                            break;
                    }
                    sectionName = "";
                    sectionData = "";
                } else if (sectionName === "") {
                    var matches = line.match(regexp);
                    if (matches) {
                        sectionName = line.match(regexp)[1].toLowerCase();
                    }
                } else {
                    sectionData += line + "\n";
                }
            });
            
            return {
                "classrooms": classrooms,
                "schedule": schedule,
                "teachers": groups,
                "classLetters": letters.sort(),
                "classNums": numbers.sort(function(a, b) {
                    if (a < b) {
                        return -1;
                    }
                    if (a > b) {
                        return 1;
                    }
                    return 0;
                }),
                "version": version
            };
        })())
    });
}

/* Обновляет уведомление */
function updateNotification() {
    console.log('Update notification!')

    if (typeof app === "undefined") {
        // Если vue ещё не инициализирован, то повторяет попытку через 3 секунды
        setTimeout(updateNotification, 3*1000);
        return;
    }

    if (!app.enableNotification) {
        document.title = 'Школьное расписание';
        app.title = 'Школьное расписание';
        cordova.plugins.backgroundMode.configure({
            title: 'Школьное расписание',
            text: 'Работает в фоне',
        });
        return;
    } else {
        // Обновляет расписание через 30 секунд
        setTimeout(updateNotification, 30*1000);
    }

    var currentLesson = null;
    var leftTime = 24 * 60;
    var isNow = false;

    DefaultFilter.DayOfWeek = GetCurrentDayOfWeek();
    var filtered = DefaultFilter.FindLessons(app.schedule);
    for (var i = 0; i < filtered.length; i++) {
        var lesson = filtered[i];
        if (lesson.IsNow()) {
            currentLesson = lesson;
            leftTime = currentLesson.GetLeftTime();
            isNow = true;
            break;
        } else {
            var lessonLeftTime = lesson.GetLeftTime();
            if (lessonLeftTime > 0 && lessonLeftTime < leftTime) {
                leftTime = lessonLeftTime;
                currentLesson = lesson;
            }
        }
    }
    if (currentLesson === null) {
        document.title = 'Больше уроков нет';
        app.title = 'Больше уроков нет';
        cordova.plugins.backgroundMode.configure({
            title: 'Школьное расписание',
            text: 'Больше уроков нет'
        });
        return;
    }
    if (!isNow && leftTime > 30) {
        document.title = 'Уроки ещё не скоро';
        app.title = 'Уроки ещё не скоро';
        cordova.plugins.backgroundMode.configure({
            title: 'Школьное расписание',
            text: 'Уроки ещё не скоро'
        });
        return;
    }

    var time = new Date();
    var title = leftTime + " м. до " + (isNow ? "конца": "начала");
    var content = currentLesson.Classroom + " каб., " + currentLesson.LessonName;
    var total = title + ". " + content;
    app.title = total;

    document.title = total;
    cordova.plugins.backgroundMode.configure({
        title: title,
        text: content
    });
}

// Когда cordova полностью инициализируется, настраиваю в зависимости от платформы.
document.addEventListener("deviceready", onDeviceReady, false);
function onDeviceReady() {
    if (device.platform === "browser") {
        function reloadAppcache() {
            // Перезагружает страницу, если она обновилась через Html5 offline apps.
            if (window.applicationCache) {
                applicationCache.addEventListener('updateready', function() {
                    window.applicationCache.swapCache();
                    console.log("appcache updated");
                    window.location.reload();
                });
            }
        }

        // Инициализирует Service Worker
        if ('serviceWorker' in navigator) {
            navigator.serviceWorker.register('js/serviceworker.js').then(function(reg) {
                // регистрация сработала
                console.log('Registration succeeded. Scope is ' + reg.scope);
            }).catch(function(error) {
                // регистрация прошла неудачно
                console.log('Registration failed with ' + error);
                reloadAppcache();
            });
        } else {
            reloadAppcache();
        }
    }
    window.appMetrica.activate({
        apiKey: '0ab9b456-2c7f-4f12-9f38-16a9acdd1d86'
    });
    cordova.plugins.backgroundMode.setDefaults({
        title: 'Школьное расписание',
        text: 'Работает в фоне',
        vibrate: false
    });
    initApp();
    window.scrollTo(0,1);  // Убирает адресную строку на телефонах
    loadData(updateData);
    updateNotification();
}