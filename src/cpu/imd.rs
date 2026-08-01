pub struct InterruptMasterDispatcher {
    value: bool,
    target: bool,
    counter: i32,
}

impl InterruptMasterDispatcher {
    pub fn new(value: bool) -> Self {
        Self {
            value,
            target: value,
            counter: -1,
        }
    }

    pub fn value(&self) -> bool {
        self.value
    }

    pub fn enque(&mut self, value: bool, number_invocations: i32) {
        self.target = value;
        self.counter = number_invocations;
    }

    pub fn force(&mut self, value: bool) {
        self.value = value;
        self.target = value;
        self.counter = -1;
    }

    pub fn update(&mut self) {
        if self.counter < 0 {
            return;
        }

        if self.counter == 0 {
            self.value = self.target;
        }

        self.counter = self.counter.saturating_sub(1);
    }
}
